using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Dargon.Commons.AsyncPrimitives {
   public class AsyncSemaphore {
      private readonly ConcurrentQueue<AsyncLatch> awaiterLatches = new();
      private readonly object undoLock = new object();
      private int counter; // positive = availability (signals awaiters), negative = queued waiting

      public AsyncSemaphore(int count = 0) {
         if (count < 0) {
            throw new ArgumentOutOfRangeException();
         }
         counter = count;
      }

      public int Count => counter;

      public bool TryTake() {
         var spinner = new SpinWait();
         while (true) {
            var capturedCounter = Interlocked.CompareExchange(ref counter, 0, 0);
            if (capturedCounter > 0) {
               var nextCounter = capturedCounter - 1;
               if (Interlocked.CompareExchange(ref counter, nextCounter, capturedCounter) == capturedCounter) {
                  return true;
               }
            } else {
               return false;
            }
            spinner.SpinOnce();
         }
      }

      public async Task WaitAsync(CancellationToken cancellationToken = default(CancellationToken)) {
         var capturedCounter = Interlocked2.PostDecrement(ref counter);
         if (capturedCounter > 0) {
            // decremented an availability, so done!
            return;
         }

         // no availabilities; we added an awaiting count.
         // if anyone increments the count, they have to remove an AsyncLatch and signal.
         // So our job now is to add that AsyncLatch for a Release to signal.
         var latch = new AsyncLatch();
         awaiterLatches.Enqueue(latch);

         try {
            await latch.WaitAsync(cancellationToken).ConfigureAwait(false);
         } catch (OperationCanceledException) {
            if (ResolveWaitContextUndoAsync(latch)) {
               // successfully removed
               throw;
            }

            // someone already dequeued the latch, so it's being signaled.
            await latch.WaitAsync(CancellationToken.None).ConfigureAwait(false);
         }
      }

      private bool ResolveWaitContextUndoAsync(AsyncLatch latch) {
         var spinner = new SpinWait();
         bool dequeuedSelf = false;
         lock (undoLock) {
            var maxIterations = awaiterLatches.Count;
            for (var i = 0; i < maxIterations && awaiterLatches.TryDequeue(out var item); i++) {
               if (item == latch) {
                  dequeuedSelf = true;
                  break;
               } else {
                  awaiterLatches.Enqueue(item);
               }
            }
         }

         if (!dequeuedSelf) {
            // we failed to dequeue ourselves, meaning someone else already dequeued us. In that case,
            // we have been signaled so rather than cancelling the wait, we complete.
            return false;
         }

         // below, we have successfully dequeued ourselves.
         while (true) {
            var capturedCounter = Interlocked2.Read(ref counter);
            if (capturedCounter >= 0) {
               // the counter indicates no pending awaiters, meaning a Release is waiting to take our latch.
               awaiterLatches.Enqueue(latch);
               return false;
            } else {
               // the counter indicates queued awaiters. If we can removed a queued awaiter count via increment,
               // then we have undone the latch add
               var nextCounter = capturedCounter + 1;
               if (Interlocked.CompareExchange(ref counter, nextCounter, capturedCounter) == capturedCounter) {
                  return true;
               }
               spinner.SpinOnce();
            }
         }
      }

      public void Release(int c) {
         for (var i = 0; i < c; i++) {
            Release();
         }
      }

      public void Release() {
         var capturedCounter = Interlocked2.PostIncrement(ref counter);
         if (capturedCounter >= 0) {
            // transition from neutral to availability or from availability to availability.
            // no need to signal awaiters.
            return;
         }

         // transition from pending to pending or pending to neutral... signal an awaiter.
         var spinner = new SpinWait();
         while (true) {
            AsyncLatch latch;
            while (!awaiterLatches.TryDequeue(out latch)) {
               spinner.SpinOnce();
            }

            latch.SetOrThrow();
            return;
         }
      }
   }
}
