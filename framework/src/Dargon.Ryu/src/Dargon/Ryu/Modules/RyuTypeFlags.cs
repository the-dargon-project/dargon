using System;

namespace Dargon.Ryu.Modules {
   [Flags]
   public enum RyuTypeFlags : uint {
      None              = 0,
      Required          = 0x00000001U,
      Cache             = 0x00000002U
   }
}
