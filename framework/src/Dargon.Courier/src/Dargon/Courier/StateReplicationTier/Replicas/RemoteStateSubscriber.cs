using System;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Commons.AsyncPrimitives;
using Dargon.Courier.PeeringTier;
using Dargon.Courier.PubSubTier.Subscribers;
using Dargon.Courier.PubSubTier.Vox;
using Dargon.Courier.ServiceTier.Client;
using Dargon.Courier.StateReplicationTier.Primaries;
using Dargon.Courier.StateReplicationTier.States;
using Dargon.Courier.StateReplicationTier.Vox;

namespace Dargon.Courier.StateReplicationTier.Replicas {
   public class RemoteStateSubscriber<TState, TSnapshot, TDelta> where TState : class, IState where TSnapshot : IStateSnapshot where TDelta : class, IStateDelta {
      private readonly RemoteServiceProxyContainer remoteServiceProxyContainer;
      private readonly Subscriber subscriber;
      private readonly PeerContext remote;
      private readonly Guid topicId;
      private readonly StateUpdateProcessor<TState, TSnapshot, TDelta> updateProcessor;

      private readonly AsyncLatch disposeLatch = new();

      public RemoteStateSubscriber(RemoteServiceProxyContainer remoteServiceProxyContainer, Subscriber subscriber, PeerContext remote, Guid topicId, StateUpdateProcessor<TState, TSnapshot, TDelta> updateProcessor) {
         this.remoteServiceProxyContainer = remoteServiceProxyContainer;
         this.subscriber = subscriber;
         this.remote = remote;
         this.topicId = topicId;
         this.updateProcessor = updateProcessor;
      }

      public async Task InitializeAsync() {
         await subscriber.SubscribeToRemoteTopicAsync(topicId, remote, HandleRemotePublishAsync);
         var primaryStateService = remoteServiceProxyContainer.Get<IStateSnapshotProviderService<TState, TSnapshot, TDelta>>(topicId, remote);
         var update = await primaryStateService.GetOutOfBandSnapshotUpdateOfLatestStateAsync();
         updateProcessor.Enqueue(update);
      }

      private Task HandleRemotePublishAsync(PubSubNotification notification) {
         var stateUpdate = (StateUpdateDto)notification.Payload;
         updateProcessor.Enqueue(stateUpdate);
         return Task.CompletedTask;
      }

      public void Dispose() {
         if (!disposeLatch.TrySet()) return;
         subscriber.UnsubscribeFromRemoteTopicAsync(topicId, remote).Forget();
      }
   }
}