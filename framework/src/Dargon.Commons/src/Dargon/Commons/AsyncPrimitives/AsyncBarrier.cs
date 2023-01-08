using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text; 
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons.Collections;
using Dargon.Commons.Exceptions;

namespace Dargon.Commons.AsyncPrimitives {
   /// <summary>
   /// This probably isn't the most performant implementation, but we don't use barrier anywhere
   /// in the codebase except for test so it's not a big deal.
   /// </summary>
   public class AsyncBarrier {
      private readonly ConcurrentQueue<AsyncLatch> latchQueue = new();
      private readonly int signalsPerBarrier;
      private int counter;

      public AsyncBarrier(int signalsPerBarrier) {
         this.signalsPerBarrier = signalsPerBarrier.AssertIsGreaterThan(0);
      }

      public Task SignalAndWaitAsync() {
         var next = Interlocked2.PreIncrementWithMod(ref counter, signalsPerBarrier);

         if (next == 0) {
            // we are the nth increment, signal the prior ones
            var spinner = new SpinWait();
            for (var i = 1; i < signalsPerBarrier; i++) {
               AsyncLatch latch;
               while (!latchQueue.TryDequeue(out latch)) {
                  spinner.SpinOnce();
               }

               latch.SetOrThrow();
            }

            return Task.CompletedTask;
         } else {
            // we are not the nth increment, so we will be signalled.
            var latch = new AsyncLatch();
            latchQueue.Enqueue(latch);
            return latch.WaitAsync();
         }
      }
   }
}
