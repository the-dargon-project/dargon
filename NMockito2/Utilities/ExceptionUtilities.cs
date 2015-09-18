using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;

namespace NMockito2.Utilities {
   public static class ExceptionUtilities {
      public static void Rethrow(this Exception e) => ExceptionDispatchInfo.Capture(e).Throw();
   }
}
