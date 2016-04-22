using Dargon.Courier.Vox;

namespace Dargon.Courier.PeeringTier {
   public class PeerDiscoveryEvent {
      public PeerContext Peer { get; set; }
      public InboundPayloadEvent PayloadEvent { get; set; }
      public AnnouncementDto Announcement { get; set; }
   }
}