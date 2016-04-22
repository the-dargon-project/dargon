using System;

namespace Dargon.Courier.Vox {
   [Flags]
   public enum PacketFlags {
      None = 0,
      Reliable = 1,
   }
}