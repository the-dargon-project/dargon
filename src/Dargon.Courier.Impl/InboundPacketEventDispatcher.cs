using System;
using System.Threading.Tasks;
using Dargon.Courier.AsyncPrimitives;
using Dargon.Courier.PacketTier;
using Dargon.Courier.Vox;
using NLog;

namespace Dargon.Courier {
   public class InboundPacketEventDispatcher {
      private static readonly Logger logger = LogManager.GetCurrentClassLogger();
      private readonly DuplicateFilter duplicateFilter;
      private readonly Acknowledger acknowledger;
      private readonly InboundPacketEventRouter inboundPacketEventRouter;

      public InboundPacketEventDispatcher(DuplicateFilter duplicateFilter, Acknowledger acknowledger, InboundPacketEventRouter inboundPacketEventRouter) {
         this.duplicateFilter = duplicateFilter;
         this.acknowledger = acknowledger;
         this.inboundPacketEventRouter = inboundPacketEventRouter;
      }

      public Task DispatchAsync(InboundPacketEvent e) {
         if (duplicateFilter.IsNew(e.Packet.Id)) {
            logger.Trace($"Got new packet of id {e.Packet.Id}.");
            return Task.WhenAll(
               AcknowledgeAsync(e),
               RouteAsync(e));
         }
         logger.Trace($"Filtered duplicate packet of id {e.Packet.Id}.");
         return Task.FromResult(false);
      }

      private async Task AcknowledgeAsync(InboundPacketEvent e) {
         if (e.Packet.IsReliable()) {
            await acknowledger.AcknowledgeAsync(e);
         }
      }

      private async Task RouteAsync(InboundPacketEvent e) {
         await inboundPacketEventRouter.TryRouteAsync(e);
      }
   }
}