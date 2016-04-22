using System.Threading.Tasks;
using Dargon.Courier.AsyncPrimitives;
using Dargon.Courier.PeeringTier;
using Dargon.Courier.Vox;

namespace Dargon.Courier {
   public class InboundMessageDispatcher {
      private readonly PeerTable peerTable;
      private readonly InboundMessageRouter inboundMessageRouter;

      public InboundMessageDispatcher(PeerTable peerTable, InboundMessageRouter inboundMessageRouter) {
         this.peerTable = peerTable;
         this.inboundMessageRouter = inboundMessageRouter;
      }

      public async Task DispatchAsync<T>(InboundMessageEvent<T> e) {
         var peer = peerTable.GetOrAdd(e.Message.SenderId);
         await peer.WaitForDiscoveryAsync();

         e.Sender = peer;

         await inboundMessageRouter.RouteAsync(e);

         e.Sender = null;
      }
   }
}