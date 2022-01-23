using System;
using Dargon.Commons;
using Dargon.Courier.PeeringTier;
using Dargon.Courier.PubSubTier.Publishers;
using Dargon.Courier.PubSubTier.Subscribers;
using Dargon.Courier.StateReplicationTier.Predictions;
using Dargon.Courier.StateReplicationTier.Primaries;
using Dargon.Courier.StateReplicationTier.Replicas;
using Dargon.Courier.StateReplicationTier.States;

namespace Dargon.Courier.StateReplicationTier {
   public class StateViewFactory<TState, TSnapshot, TDelta, TOperations, TSelf>
      where TState : class, IState
      where TSnapshot : IStateSnapshot
      where TDelta : class, IStateDelta
      where TOperations : IStateDeltaOperations<TState, TSnapshot, TDelta> {

      private readonly TOperations ops;
      private readonly Publisher publisher;
      private readonly Subscriber subscriber;

      public StateViewFactory(TOperations ops, Publisher publisher, Subscriber subscriber) {
         this.ops = ops;
         this.publisher = publisher;
         this.subscriber = subscriber;
      }

      public PrimaryStateView CreatePrimaryStateView(TState state, Guid topicId) {
         var statePublisher = new StatePublisher(publisher, ops, topicId);
         var primaryStateView = new PrimaryStateView(state, ops, statePublisher);
         statePublisher.Initialize(primaryStateView);
         return primaryStateView;
      }

      public ReplicaStateView CreateReplicaStateView(TState state, PeerContext peer, Guid topicId) {
         var stateUpdateProcessor = new StateUpdateProcessor<TState, TSnapshot, TDelta>(
            state,
            ops);
         var remoteStateSubscriber = new RemoteStateSubscriber<TState, TSnapshot, TDelta>(
            subscriber,
            peer,
            topicId,
            stateUpdateProcessor);
         remoteStateSubscriber.InitializeAsync().Forget();
         var replicaStateView = new ReplicaStateView(state, remoteStateSubscriber, stateUpdateProcessor);
         return replicaStateView;
      }

      public PredictedStateView CreatePredictedStateView(IStateView<TState> baseStateView) {
         return new PredictedStateView(baseStateView, ops);
      }

      public class PrimaryStateView : PrimaryStateView<TState, TSnapshot, TDelta, TOperations> {
         public PrimaryStateView(TState state, TOperations ops, StatePublisher<TState, TSnapshot, TDelta, TOperations> publisher) : base(state, ops, publisher) { }
      }

      private class StatePublisher : StatePublisher<TState, TSnapshot, TDelta, TOperations> {
         public StatePublisher(Publisher publisher, TOperations ops, Guid topicId) : base(publisher, ops, topicId) { }
      }

      public class ReplicaStateView : ReplicaStateView<TState, TSnapshot, TDelta> {
         public ReplicaStateView(TState state, RemoteStateSubscriber<TState, TSnapshot, TDelta> remoteStateSubscriber, StateUpdateProcessor<TState, TSnapshot, TDelta> stateUpdateProcessor) : base(state, remoteStateSubscriber, stateUpdateProcessor) { }
      }

      public class PredictedStateView : PredictedStateView<TState, TSnapshot, TDelta, TOperations> {
         public PredictedStateView(IStateView<TState> baseStateView, TOperations ops) : base(baseStateView, ops) { }
      }
   }
}