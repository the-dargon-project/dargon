using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dargon.Courier.Messaging {
   public enum MessageFlags : uint {
      None = 0, 
      AcknowledgementRequired = 1,
      Default = None,
   }
}
