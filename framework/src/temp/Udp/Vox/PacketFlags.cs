using System;

namespace Dargon.Courier.TransportTier.Udp.Vox {
   [Flags]
   public enum PacketFlags {
      None = 0,
      Reliable = 1,
   }
}