using System.Threading.Tasks;
using Dargon.Courier.Vox;

namespace Dargon.Courier.PeeringTier {
   public class PeerAnnouncementHandler {
      private readonly PeerTable peerTable;

      public PeerAnnouncementHandler(PeerTable peerTable) {
         this.peerTable = peerTable;
      }

      public async Task HandleAnnouncementAsync(InboundPayloadEvent e) {
         var announcement = (AnnouncementDto)e.Payload;
         var peerContext = peerTable.GetOrAdd(announcement.Identity.Id);
         await peerContext.ProcessAsync(e);
      }
   }
}