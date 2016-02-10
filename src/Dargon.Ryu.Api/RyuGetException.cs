using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dargon.Ryu {
   public class RyuGetException : Exception {
      public RyuGetException(Type gettingType, Exception innerException)
         : base("While constructing " + gettingType.FullName, innerException) { }
   }
   public class RyuActivateException : Exception {
      public RyuActivateException(Type activatingType, Exception innerException)
         : base("While activating " + activatingType.FullName, innerException) { }
   }
}
