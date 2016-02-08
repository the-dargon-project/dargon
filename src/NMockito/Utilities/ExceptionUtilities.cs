using System;
using System.Runtime.ExceptionServices;

namespace NMockito.Utilities {
   public static class ExceptionUtilities {
      public static void Rethrow(this Exception e) => ExceptionDispatchInfo.Capture(e).Throw();
   }
}
