using System;
using System.Threading.Tasks;
using Dargon.Courier.PubSubTier.Publishers;
using Dargon.Courier.ServiceTier.Server;

namespace Dargon.Courier.PubSubTier {
   public class PubSubService : IPubSubService {
      private readonly LocalTopicsTable localTopicsTable;

      public PubSubService(LocalTopicsTable localTopicsTable) {
         this.localTopicsTable = localTopicsTable;
      }

      public async Task SubscribeAsync(Guid topicId) {
         var peer = CourierGlobals.AlsCurrentInboundMessagePeer;
         await localTopicsTable.AddSubscriptionAsync(topicId, peer);
      }

      public async Task UnsubscribeAsync(Guid topicId) {
         var peer = CourierGlobals.AlsCurrentInboundMessagePeer;
         await localTopicsTable.RemoveSubscriptionAsync(topicId, peer);
      }
   }
}