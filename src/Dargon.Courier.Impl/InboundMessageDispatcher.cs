using System;
using System.Threading.Tasks;
using Dargon.Courier.AsyncPrimitives;
using Dargon.Courier.PeeringTier;
using Dargon.Courier.Vox;

namespace Dargon.Courier {
   public class InboundMessageDispatcher {
      private readonly Identity identity;
      private readonly PeerTable peerTable;
      private readonly InboundMessageRouter inboundMessageRouter;

      public InboundMessageDispatcher(Identity identity, PeerTable peerTable, InboundMessageRouter inboundMessageRouter) {
         this.identity = identity;
         this.peerTable = peerTable;
         this.inboundMessageRouter = inboundMessageRouter;
      }

      public async Task DispatchAsync<T>(InboundMessageEvent<T> e) {
         var message = e.Message;
         if (message.ReceiverId != Guid.Empty &&
             message.ReceiverId != identity.Id) {
            return;
         }

         var peer = peerTable.GetOrAdd(message.SenderId);
         await peer.WaitForDiscoveryAsync();

         e.Sender = peer;

         await inboundMessageRouter.RouteAsync(e);

         e.Sender = null;
      }
   }
}