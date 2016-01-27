using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dargon.Ryu {
   [Flags]
   public enum RyuTypeFlags : uint {
      None              = 0,
      Required          = 0x00000001U,
      IgnoreDuplicates  = 0x00000002U,
      Service           = 0x00000004U,
      ManagementObject  = 0x00000008U,
      PofContext        = 0x00000010U,
      Cache             = 0x00000020U
   }
}
