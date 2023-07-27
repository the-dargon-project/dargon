using System;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Commons.AsyncAwait;
using Dargon.Courier.PeeringTier;
using Dargon.Courier.PubSubTier.Publishers;
using Dargon.Courier.PubSubTier.Subscribers;
using Dargon.Courier.ServiceTier.Client;
using Dargon.Courier.ServiceTier.Server;
using Dargon.Courier.StateReplicationTier.Predictions;
using Dargon.Courier.StateReplicationTier.Primaries;
using Dargon.Courier.StateReplicationTier.Replicas;
using Dargon.Courier.StateReplicationTier.States;
using Dargon.Ryu;

namespace Dargon.Courier.StateReplicationTier {
   public class ViewFactoryIocDependencies {
      public CourierFacade Courier { get; set; }
   }

   public class StateBase<TState, TSnapshot, TDelta, TOperations> : ThreadLocalContext<TState>, IState
      where TState : ThreadLocalContext<TState>, IState
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

         public PrimaryStateView CreatePrimaryStateView(TState state, Guid topicId, StateLock stateLock) {
            using (courierSynchronizationContexts.CourierDefault__.ActivateTemporarily()) {
               var statePublisher = new StatePublisher(courierSynchronizationContexts, publisher, ops, topicId, stateLock);
               var primaryStateView = new PrimaryStateView(state, ops, statePublisher, stateLock);
               statePublisher.InitializeAsync(primaryStateView, stateLock).Forget();
               localServiceRegistry.RegisterService<IPrimaryStateService<TState, TSnapshot, TDelta>>(
                  topicId,
                  new PrimaryStateService<TState, TSnapshot, TDelta, TOperations>(primaryStateView, ops, statePublisher, stateLock));
               return primaryStateView;
            }
         }

         public ReplicaStateView CreateReplicaStateView(TState state, PeerContext peer, Guid topicId) {
            using (courierSynchronizationContexts.CourierDefault__.ActivateTemporarily()) {
               var stateUpdateProcessor = new StateUpdateProcessor<TState, TSnapshot, TDelta>(
                  state,
                  ops);
               var remoteStateSubscriber = new RemoteStateSubscriber<TState, TSnapshot, TDelta>(
                  remoteServiceProxyContainer,
                  subscriber,
                  peer,
                  topicId,
                  stateUpdateProcessor);
               remoteStateSubscriber.InitializeAsync().Forget();
               var replicaStateView = new ReplicaStateView(state, remoteStateSubscriber, stateUpdateProcessor);
               return replicaStateView;
            }
         }

         public async Task<ReplicaStateView> CreateReplicaStateViewAndWaitForInitialStateAsync(TState state, PeerContext peer, Guid topicId) {
            await courierSynchronizationContexts.CourierDefault__.YieldToAsync();
            var rsv = CreateReplicaStateView(state, peer, topicId);
            await rsv.WaitForAndProcessInitialStateUpdateAsync();
            return rsv;
         }


         public PredictionStateView CreatePredictionStateView(IStateView<TState> baseStateView) {
            return new PredictionStateView(baseStateView, ops);
         }
      }

      public class PrimaryStateView : PrimaryStateView<TState, TSnapshot, TDelta, TOperations> {
         public PrimaryStateView(TState state, TOperations ops, StatePublisher<TState, TSnapshot, TDelta, TOperations> publisher, StateLock stateLock) : base(state, ops, publisher, stateLock) { }
      }

      private class StatePublisher : StatePublisher<TState, TSnapshot, TDelta, TOperations> {
         public StatePublisher(CourierSynchronizationContexts synchronizationContexts, Publisher publisher, TOperations ops, Guid topicId, StateLock stateLock) : base(synchronizationContexts, publisher, ops, topicId) { }
      }

      public class ReplicaStateView : ReplicaStateView<TState, TSnapshot, TDelta> {
         public ReplicaStateView(TState state, RemoteStateSubscriber<TState, TSnapshot, TDelta> remoteStateSubscriber, StateUpdateProcessor<TState, TSnapshot, TDelta> stateUpdateProcessor) : base(state, remoteStateSubscriber, stateUpdateProcessor) { }
      }

      public class PredictionStateView : PredictionStateView<TState, TSnapshot, TDelta, TOperations> {
         public PredictionStateView(IStateView<TState> baseStateView, TOperations ops) : base(baseStateView, ops) { }
      }
   }
}