using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dargon.Commons.AsyncPrimitives {
   public struct AsyncSpinner {
      private readonly int maxSleepMillis;
      private int numSpins;

      public AsyncSpinner(int maxSleepMillis = 1000) {
         this.maxSleepMillis = maxSleepMillis;
         this.numSpins = 0;
      }

      public async Task SpinAsync() {
         if (numSpins < 4) {
            await Task.Yield();
            numSpins++;
         } else {
            var sleepMillis = Math.Min(maxSleepMillis, Math.Pow(2, numSpins - 4));
            await Task.Delay((int)sleepMillis);

            if (numSpins < 20) {
               numSpins++;
            }
         }
      }
   }
}
