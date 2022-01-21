using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Courier.PeeringTier;
using Dargon.Courier.PubSubTier.Publishers;
using Dargon.Courier.PubSubTier.Vox;
using Dargon.Courier.ServiceTier.Client;

namespace Dargon.Courier.PubSubTier.Subscribers {
   internal interface IInternalSubscriberInterfaceConstraint {
      Task SubscribeToRemoteTopicAsync(Guid topicId, PeerContext peer, PubSubCallbackFunc callback);
      Task UnsubscribeFromRemoteTopicAsync(Guid topicId, PeerContext peer);
   }

   public class Subscriber : IInternalSubscriberInterfaceConstraint {
      private readonly InboundMessageRouter inboundMessageRouter;
      private readonly RemoteServiceProxyContainer remoteServiceProxyContainer;
      private readonly ConcurrentDictionary<Guid, RemoteToLocalSubscriptionContext> remoteToLocalSubscriptionContextsByTopic = new();

      public Subscriber(InboundMessageRouter inboundMessageRouter, RemoteServiceProxyContainer remoteServiceProxyContainer) {
         this.inboundMessageRouter = inboundMessageRouter;
         this.remoteServiceProxyContainer = remoteServiceProxyContainer;
         this.inboundMessageRouter.RegisterHandler<PubSubNotification>(HandlePubSubNotification);
      }

      public async Task SubscribeToRemoteTopicAsync(Guid topicId, PeerContext peer, PubSubCallbackFunc callback) {
         remoteToLocalSubscriptionContextsByTopic.AddOrThrow(topicId, new RemoteToLocalSubscriptionContext {
            ValidPublisher = peer,
            Callback = callback,
         });

         var pubsub = remoteServiceProxyContainer.Get<IPubSubService>(peer);
         await pubsub.SubscribeAsync(topicId);
      }

      public async Task UnsubscribeFromRemoteTopicAsync(Guid topicId, PeerContext peer) {
         remoteToLocalSubscriptionContextsByTopic.RemoveOrThrow(topicId);

         var pubsub = remoteServiceProxyContainer.Get<IPubSubService>(peer);
         await pubsub.UnsubscribeAsync(topicId);
      }

      private Task HandlePubSubNotification(IInboundMessageEvent<PubSubNotification> e) {
         var topic = e.Body.Topic;

         // validate sender
         var remoteToLocalSubscriptionContext = remoteToLocalSubscriptionContextsByTopic[topic];
         Assert.Equals(remoteToLocalSubscriptionContext.ValidPublisher, e.Sender);

         // run callback
         return remoteToLocalSubscriptionContext.Callback(e.Body);
      }

      private class RemoteToLocalSubscriptionContext {
         public PeerContext ValidPublisher;
         public PubSubCallbackFunc Callback;
      }
   }
}
