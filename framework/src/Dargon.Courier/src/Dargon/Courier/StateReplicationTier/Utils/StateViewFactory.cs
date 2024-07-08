using System;
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
using Dargon.Courier.StateReplicationTier.Shared;
using Dargon.Courier.StateReplicationTier.States;
using Dargon.Ryu.Attributes;

namespace Dargon.Courier.StateReplicationTier.Utils;

[RyuDoNotAutoActivate]
public class ViewFactoryIocDependencies {
   public CourierFacade Courier { get; set; }
}

/// <summary>
/// StateBase refers to state, snapshot, and delta but not a concrete Operations, because
/// StateBase is intended to be in the Schema namespace which doesn't necessarily have access to game logic.
/// </summary>
public class StateBase<TState, TSnapshot, TDelta> : /* ThreadLocalContext<TState>, */ IState
   where TState : /*ThreadLocalContext<TState>, */ class, IState
   where TSnapshot : IStateSnapshot
   where TDelta : class, IStateDelta {
   
   public interface IOperations : IStateDeltaOperations<TState, TSnapshot, TDelta>;

   public class ViewFactory {
      private readonly CourierSynchronizationContexts courierSynchronizationContexts;
      private readonly IOperations ops;
      private readonly LocalServiceRegistry localServiceRegistry;
      private readonly RemoteServiceProxyContainer remoteServiceProxyContainer;
      private readonly Publisher publisher;
      private readonly Subscriber subscriber;

      [RyuConstructor]
      public ViewFactory(ViewFactoryIocDependencies deps, IOperations ops) : this(deps.Courier, ops) { }

      public ViewFactory(CourierFacade courier, IOperations ops) : this(courier.SynchronizationContexts, ops, courier.LocalServiceRegistry, courier.RemoteServiceProxyContainer, courier.Publisher, courier.Subscriber) { }

      public ViewFactory(CourierSynchronizationContexts courierSynchronizationContexts, IOperations ops, LocalServiceRegistry localServiceRegistry, RemoteServiceProxyContainer remoteServiceProxyContainer, Publisher publisher, Subscriber subscriber) {
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
         var predictor = new StatePredictorCore<TState, TSnapshot, TDelta>(baseView, predictionView);
         return new Predictor(predictionView, predictor);
      }

      public StateFilterPipeline CreateFilterPipeline(StateView src, StateView dst, IStateFilter filter) {
         var res = new StateFilterPipeline(src, dst, filter);
         res.Initialize();
         return res;
      }
   }

   public class StateView : StateView<TState, TSnapshot, TDelta, IOperations> {
      public StateView(TState state, IOperations ops) : base(state, ops) { }
   }

   public class PrimaryStateView : StateView {
      public PrimaryStateView(TState state, IOperations ops) : base(state, ops) { }
   }

   public class PublisherContext : IDisposable {
      private readonly AsyncLatch disposeLatch = new();
      private readonly Guid topicId;
      private readonly LocalServiceRegistry localServiceRegistry;

      private readonly StatePublisher<TState, TSnapshot, TDelta> statePublisher;
      private readonly StateSnapshotProviderService<TState, TSnapshot, TDelta, IOperations> snapshotProviderService;

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

   public class Predictor(StateView view, StatePredictorCore<TState, TSnapshot, TDelta> inner) {
      public StateView View => view;

      public void ProcessUpdates() => inner.ProcessUpdates();

      // public SyncStateGuard<TState> LockStateForRead() {
      //    return view.LockStateForRead();
      // }

      public Task<AsyncStateGuard<TState>> LockStateForReadAsync() => view.LockStateForReadAsync();

      //public Task<AsyncStateGuard<TState>> LockStateForReadAsync() => view.LockStateForWriteAsync();

      // public SyncStateGuard<TState> ProcessUpdatesAndLockStateForRead() {
      //    inner.ProcessUpdates();
      //    return view.LockStateForRead();
      // }

      public Task<AsyncStateGuard<TState>> ProcessUpdatesAndLockStateForReadAsync() {
         inner.ProcessUpdates();
         return view.LockStateForReadAsync();
      }

      public async Task<bool> AddPredictionAsync(IProposal<TState, TDelta> prediction) {
         await using var _ = await view.LockStateForWriteAsync();
         return inner.AddPrediction(prediction);
      }

      public async Task<bool> RemovePredictionAsync(IProposal<TState, TDelta> prediction) {
         await using var _ = await view.LockStateForWriteAsync();
         return inner.RemovePrediction(prediction);
      }
   }

   public interface IStateFilter : IStateFilter<TState, TSnapshot, TDelta>;

   public class StateFilterPipeline(StateView src, StateView dst, IStateFilter filter)
      : StateFilterPipeline<TState, TSnapshot, TDelta>(src, dst, filter);

   public interface IProposal : IProposal<TState, TDelta>;

   public interface IProposer : IProposer<TState, TSnapshot, TDelta, IProposal>;

   public class ProposalIngest : IProposer {
      private readonly PrimaryStateView primary;

      private readonly Predictor predictor;
      private readonly IProposer remote;

      public ProposalIngest(PrimaryStateView primary) {
         this.primary = primary;
      }

      public ProposalIngest(Predictor predictor, IProposer remote) {
         this.predictor = predictor;
         this.remote = remote;
      }

      public async Task<bool> TryApplyAsync(IProposal proposal) {
         if (primary != null) {
            await using var guard = await primary.LockStateForWriteAsync();
            var res = proposal.TryBuildDelta(guard.State, out var delta);
            if ((res & Result.DropSelf) != 0) {
               return false;
            }
            
            return await primary.TryApplyDeltaAsync(delta);
         } else {
            var ok = await predictor.AddPredictionAsync(proposal);
            ok = await remote.TryApplyAsync(proposal);
            if (!ok) {
               await predictor.RemovePredictionAsync(proposal);
            }
            return ok;
         }
      }
   }
}