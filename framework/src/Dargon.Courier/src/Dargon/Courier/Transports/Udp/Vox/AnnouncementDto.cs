using Dargon.Courier.PeeringTier;
using Dargon.Vox;

namespace Dargon.Courier.TransportTier.Udp.Vox {
   [AutoSerializable]
   public class AnnouncementDto {
      public WhoamiDto WhoAmI { get; set; }
   }
}