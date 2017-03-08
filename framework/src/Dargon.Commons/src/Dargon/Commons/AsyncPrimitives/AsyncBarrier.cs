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
      private readonly AsyncLock synchronization = new AsyncLock();
      private readonly ConcurrentSet<AsyncLatch> waitLatches = new ConcurrentSet<AsyncLatch>();
      private readonly int totalSignallers;

      public AsyncBarrier(int totalSignallers) {
         if (totalSignallers < 0) {
            throw new ArgumentException(nameof(totalSignallers));
         }
         this.totalSignallers = totalSignallers;
      }

      public async Task SignalAndWaitAsync(CancellationToken cancellationToken = default(CancellationToken)) {
         AsyncLatch waitLatch = null;

         using (await synchronization.LockAsync(cancellationToken)) {
            if (waitLatches.Count == totalSignallers - 1) {
               foreach (var signalee in waitLatches) {
                  if (!signalee.TrySet()) {
                     throw new ImpossibleStateException();
                  }
               }
               waitLatches.Clear();
            } else {
               waitLatch = new AsyncLatch();
               waitLatches.AddOrThrow(waitLatch);
            }
         }

         if (waitLatch != null) {
            try {
               await waitLatch.WaitAsync(cancellationToken);
            } catch (OperationCanceledException) {
               using (await synchronization.LockAsync(cancellationToken)) {
                  if (waitLatches.TryRemove(waitLatch)) {
                     // If removal succeeds, signalling hasn't happened yet so we can cancel.
                     throw;
                  }
               }
            }
         }
      }
   }
}
