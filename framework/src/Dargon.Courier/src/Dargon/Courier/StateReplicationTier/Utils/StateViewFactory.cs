using System;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Commons.AsyncAwait;
using Dargon.Commons.AsyncPrimitives;
using Dargon.Courier.PeeringTier;
using Dargon.Courier.PubSubTier.Publishers;
using Dargon.Courier.PubSubTier.Subscribers;
using Dargon.Courier.ServiceTier.Client;
using Dargon.Courier.ServiceTier.Server;
using Dargon.Courier.StateReplicationTier.Filters;
using Dargon.Courier.StateReplicationTier.Predictions;
using Dargon.Courier.StateReplicationTier.Primaries;
using Dargon.Courier.StateReplicationTier.Replicas;
using Dargon.Courier.StateReplicationTier.States;
using Dargon.Ryu;
using Dargon.Ryu.Attributes;

namespace Dargon.Courier.StateReplicationTier.Utils;

[RyuDoNotAutoActivate]
public class ViewFactoryIocDependencies {
   public CourierFacade Courier { get; set; }
}

public class StateBase<TState, TSnapshot, TDelta, TOperations> : /* ThreadLocalContext<TState>, */ IState
   where TState : /*ThreadLocalContext<TState>, */ class, IState
   where TSnapshot : IStateSnapshot
   where TDelta : class, IStateDelta
   where TOperations : IStateDeltaOperations<TState, TSnapshot, TDelta> {

   public class ViewFactory {
      private readonly CourierSynchronizationContexts courierSynchronizationContexts;
      private readonly TOperations ops;
      private readonly LocalServiceRegistry localServiceRegistry;
      private readonly RemoteServiceProxyContainer remoteServiceProxyContainer;
      private readonly Publisher publisher;
      private readonly Subscriber subscriber;

      [RyuConstructor]
      public ViewFactory(ViewFactoryIocDependencies deps, TOperations ops) : this(deps.Courier, ops) { }

      public ViewFactory(CourierFacade courier, TOperations ops) : this(courier.SynchronizationContexts, ops, courier.LocalServiceRegistry, courier.RemoteServiceProxyContainer, courier.Publisher, courier.Subscriber) { }

      public ViewFactory(CourierSynchronizationContexts courierSynchronizationContexts, TOperations ops, LocalServiceRegistry localServiceRegistry, RemoteServiceProxyContainer remoteServiceProxyContainer, Publisher publisher, Subscriber subscriber) {
         this.courierSynchronizationContexts = courierSynchronizationContexts;
         this.ops = ops;
         this.localServiceRegistry = localServiceRegistry;
         this.remoteServiceProxyContainer = remoteServiceProxyContainer;
         this.publisher = publisher;
         this.subscriber = subscriber;
      }

      public StateView CreateStateView(TState state) => new(state, ops);
      
      public PrimaryStateView CreatePrimaryStateView(TState state) => new(state, ops);

      public PublisherContext CreatePublisher(StateView stateView, Guid topicId) {
         using var _ = courierSynchronizationContexts.CourierDefault__.ActivateTemporarily();
         return new PublisherContext(
            stateView, courierSynchronizationContexts, publisher,
            topicId, localServiceRegistry);
      }

      public Replicator CreateReplicator(PeerContext peer, Guid topicId) {
         using var context = courierSynchronizationContexts.CourierDefault__.ActivateTemporarily();
         var stateView = CreateStateView(ops.CreateState());
         var stateUpdateProcessor = new StateUpdateProcessor<TState, TSnapshot, TDelta>(stateView);
         var remoteStateSubscriber = new RemoteStateSubscriber<TState, TSnapshot, TDelta>(
            remoteServiceProxyContainer,
            subscriber,
            peer,
            topicId,
            stateUpdateProcessor);
         remoteStateSubscriber.InitializeAsync().Forget();
         return new Replicator(stateView, stateUpdateProcessor, remoteStateSubscriber);
      }

      public async Task<Replicator> CreateReplicatorAndWaitForInitialStateAsync(PeerContext peer, Guid topicId) {
         await courierSynchronizationContexts.CourierDefault__.YieldToAsync();
         var res = CreateReplicator(peer, topicId);
         await res.WaitForAndProcessInitialStateUpdateAsync();
         return res;
      }

      public Predictor CreatePredictor(StateView baseView) {
         var predictionView = CreateStateView(ops.CreateState());
         var predictor = new StatePredictor<TState, TSnapshot, TDelta>(baseView, predictionView);
         return new Predictor(predictionView, predictor);
      }

      public StateFilterPipeline CreateFilterPipeline(StateView src, StateView dst, IStateFilter filter) {
         var res = new StateFilterPipeline(src, dst, filter);
         res.Initialize();
         return res;
      }
   }

   public class StateView : StateView<TState, TSnapshot, TDelta, TOperations> {
      public StateView(TState state, TOperations ops) : base(state, ops) { }
   }

   public class PrimaryStateView : StateView {
      public PrimaryStateView(TState state, TOperations ops) : base(state, ops) { }
   }

   public class PublisherContext : IDisposable {
      private readonly AsyncLatch disposeLatch = new();
      private readonly Guid topicId;
      private readonly LocalServiceRegistry localServiceRegistry;

      private readonly StatePublisher<TState, TSnapshot, TDelta> statePublisher;
      private readonly StateSnapshotProviderService<TState, TSnapshot, TDelta, TOperations> snapshotProviderService;

      public PublisherContext(StateView stateView, CourierSynchronizationContexts synchronizationContexts, Publisher publisher, Guid topicId, LocalServiceRegistry localServiceRegistry) {
         this.topicId = topicId;
         this.localServiceRegistry = localServiceRegistry;

         statePublisher = new(synchronizationContexts, publisher, topicId);
         statePublisher.InitializeAsync(stateView).Forget();
         
         snapshotProviderService = new(stateView, statePublisher);
         localServiceRegistry.RegisterService<IStateSnapshotProviderService<TState, TSnapshot, TDelta>>(
            topicId,
            snapshotProviderService);
      }

      public void Dispose() {
         if (!disposeLatch.TrySet()) return;
         localServiceRegistry.UnregisterService(topicId);
      }
   }

   public class Replicator(StateView stateView, StateUpdateProcessor<TState, TSnapshot, TDelta> updateProcessor, RemoteStateSubscriber<TState, TSnapshot, TDelta> remoteStateSubscriber) : IDisposable {
      public Task WaitForAndProcessInitialStateUpdateAsync() => updateProcessor.WaitForAndProcessInitialStateUpdateAsync();

      public void ProcessQueuedUpdates() => updateProcessor.ProcessQueuedUpdates();

      public SyncStateGuard<TState> ProcessQueuedUpdatesAndLockStateForRead() {
         updateProcessor.ProcessQueuedUpdates();
         return stateView.LockStateForRead();
      }

      public StateView StateView => stateView;
      public string VersionString => $"{stateView.Version}({stateView.ReplicationVersion})";

      public void Dispose() {
         remoteStateSubscriber.Dispose();
      }
   }

   public class Predictor(StateView predictionView, StatePredictor<TState, TSnapshot, TDelta> predictor) {
      public void ProcessUpdates() => predictor.ProcessUpdates();

      public SyncStateGuard<TState> LockStateForRead() {
         return predictionView.LockStateForRead();
      }

      public Task<AsyncStateGuard<TState>> LockStateForReadAsync() {
         return predictionView.LockStateForReadAsync();
      }

      public SyncStateGuard<TState> ProcessUpdatesAndLockStateForRead() {
         predictor.ProcessUpdates();
         return predictionView.LockStateForRead();
      }

      public Task<AsyncStateGuard<TState>> ProcessUpdatesAndLockStateForReadAsync() {
         predictor.ProcessUpdates();
         return predictionView.LockStateForReadAsync();
      }
   }

   public interface IStateFilter : IStateFilter<TState, TSnapshot, TDelta>;

   public class StateFilterPipeline(StateView src, StateView dst, IStateFilter filter)
      : StateFilterPipeline<TState, TSnapshot, TDelta>(src, dst, filter);
}