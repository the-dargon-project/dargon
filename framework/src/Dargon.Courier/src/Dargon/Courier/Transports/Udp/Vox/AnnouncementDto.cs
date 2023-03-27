using Dargon.Courier.PeeringTier;
using Dargon.Courier.Vox;
using Dargon.Vox2;

namespace Dargon.Courier.TransportTier.Udp.Vox {
   [VoxType((int)CourierVoxTypeIds.AnnouncementDto)]
   public class AnnouncementDto {
      public WhoamiDto WhoAmI { get; set; }
   }
}