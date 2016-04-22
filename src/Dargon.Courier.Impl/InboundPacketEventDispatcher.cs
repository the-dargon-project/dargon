using System.Threading.Tasks;
using Dargon.Courier.AsyncPrimitives;
using Dargon.Courier.PacketTier;
using Dargon.Courier.Vox;

namespace Dargon.Courier {
   public class InboundPacketEventDispatcher {
      private readonly DuplicateFilter duplicateFilter;
      private readonly Acknowledger acknowledger;
      private readonly InboundPacketEventRouter inboundPacketEventRouter;

      public InboundPacketEventDispatcher(DuplicateFilter duplicateFilter, Acknowledger acknowledger, InboundPacketEventRouter inboundPacketEventRouter) {
         this.duplicateFilter = duplicateFilter;
         this.acknowledger = acknowledger;
         this.inboundPacketEventRouter = inboundPacketEventRouter;
      }

      public Task DispatchAsync(InboundPacketEvent e) {
         return Task.WhenAll(
            AcknowledgeAsync(e),
            RouteAsync(e));
      }

      private async Task AcknowledgeAsync(InboundPacketEvent e) {
         if (!e.Packet.IsReliable())
            return;

         await acknowledger.AcknowledgeAsync(e);
      }

      private async Task RouteAsync(InboundPacketEvent e) {
         if (e.Packet.IsReliable() && !duplicateFilter.IsNew(e.Packet.Id))
            return;

         await inboundPacketEventRouter.TryRouteAsync(e);
      }
   }
}