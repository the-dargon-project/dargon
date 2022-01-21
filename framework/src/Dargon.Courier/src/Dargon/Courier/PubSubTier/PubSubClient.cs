using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Courier.PeeringTier;
using Dargon.Courier.PubSubTier.Publishers;
using Dargon.Courier.PubSubTier.Subscribers;
using Dargon.Courier.PubSubTier.Vox;
using Dargon.Courier.ServiceTier.Client;

namespace Dargon.Courier.PubSubTier {
   public class PubSubClient : IInternalPublisherInterfaceConstraint, IInternalSubscriberInterfaceConstraint {
      private readonly Publisher publisher;
      private readonly Subscriber subscriber;

      public PubSubClient(Publisher publisher, Subscriber subscriber) {
         this.publisher = publisher;
         this.subscriber = subscriber;
      }

      public Task CreateLocalTopicAsync(Guid guid)
         => publisher.CreateLocalTopicAsync(guid);

      public Task DestroyLocalTopicAsync(Guid guid)
         => publisher.DestroyLocalTopicAsync(guid);

      public Task PublishToLocalTopicAsync<T>(Guid guid, T payload)
         => publisher.PublishToLocalTopicAsync(guid, payload);

      public Task SubscribeToRemoteTopicAsync(Guid topicId, PeerContext peer, PubSubCallbackFunc callback)
         => subscriber.SubscribeToRemoteTopicAsync(topicId, peer, callback);

      public Task UnsubscribeFromRemoteTopicAsync(Guid topicId, PeerContext peer)
         => subscriber.UnsubscribeFromRemoteTopicAsync(topicId, peer);
   }
}