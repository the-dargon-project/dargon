using Dargon.Courier.PeeringTier;
using Dargon.Courier.Vox;
using Dargon.Vox2;

namespace Dargon.Courier.TransportTier.Tcp.Vox {
   [VoxType((int)CourierVoxTypeIds.HandshakeDto)]
   public class HandshakeDto {
      public WhoamiDto WhoAmI { get; set; }
   }
}
