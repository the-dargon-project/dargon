using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Dargon.Commons.Utilities {
   public class AutoStopwatch {
      private readonly Stopwatch inner;

      public AutoStopwatch() {
         inner = new Stopwatch();
         inner.Start();
      }

      public TimeSpan GetElapsedAndRestart() {
         var res = inner.Elapsed;
         inner.Restart();
         return res;
      }
   }
}
