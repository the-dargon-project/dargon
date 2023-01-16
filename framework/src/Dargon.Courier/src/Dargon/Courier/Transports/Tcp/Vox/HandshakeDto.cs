using Dargon.Courier.PeeringTier;
using Dargon.Vox;

namespace Dargon.Courier.TransportTier.Tcp.Vox {
   [AutoSerializable]
   public class HandshakeDto {
      public WhoamiDto WhoAmI { get; set; }
   }
}
