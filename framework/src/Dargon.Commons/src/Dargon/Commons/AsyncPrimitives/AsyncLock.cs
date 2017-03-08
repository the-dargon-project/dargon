using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dargon.Commons.AsyncPrimitives {
   public class AsyncLock {
      private readonly AsyncSemaphore semaphore = new AsyncSemaphore(1);

      public async Task<Guard> LockAsync(CancellationToken cancellationToken = default(CancellationToken)) {
         await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
         return new Guard(semaphore);
      }

      public class Guard : IDisposable {
         private const int kStateLockHeld = 0;
         private const int kStateLockFreed = 1;
         private const int kStateDisposed = 2;

         private AsyncSemaphore semaphore;
         private int state = kStateLockHeld;

         public Guard(AsyncSemaphore semaphore) {
            this.semaphore = semaphore;
         }

         public void Free() {
            var previous = Interlocked.CompareExchange(ref state, kStateLockHeld, kStateLockFreed);
            if (previous == kStateLockFreed) {
               throw new InvalidOperationException("Double Free Attempted!");
            } else if (previous == kStateDisposed) {
               throw new ObjectDisposedException(nameof(Guard), "Attempted to free lock guard after disposal.");
            }
            semaphore?.Release();
            semaphore = null;
         }

         public void Dispose() {
            var previous = Interlocked.CompareExchange(ref state, kStateLockHeld, kStateLockFreed);
            if (previous == kStateLockHeld) {
               semaphore?.Release();
               semaphore = null;
            }
         }
      }
   }
}
