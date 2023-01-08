using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dargon.Commons.AsyncPrimitives {
   public class AsyncLock {
      private readonly AsyncSemaphore semaphore = new AsyncSemaphore(1);
      private readonly AsyncLocal<int> alsLockDepth = new();

      public int DebugLockDepth => alsLockDepth.Value;

      /// <summary>
      /// See comments in <see cref="AsyncReaderWriterLock.CreateReaderGuardAsync"/> for why
      /// this method cannot be marked as async.
      /// </summary>
      public Task<Guard> LockAsync() {
         alsLockDepth.Value++;
         return LockAsync_InternalAsync(CancellationToken.None);
      }

      /// <summary>
      /// If this method throws, the caller must either:
      /// 1. Not invoke <see cref="LockAsync"/> of this instance again within its current async ExecutionContext (effectively
      ///    the current `async` method scope)
      /// 2. Invoke NotifyThatLockAsyncUnsafeThrew if and only if this specific method threw the cancellation.
      /// </summary>
      public Task<Guard> LockAsyncUnsafe_WithImportantCaveats(CancellationToken cancellationToken) {
         alsLockDepth.Value++;
         return LockAsync_InternalAsync(cancellationToken);
      }

      public void NotifyThatLockAsyncUnsafeThrew() {
         alsLockDepth.Value--;
         alsLockDepth.Value.AssertIsGreaterThanOrEqualTo(0);
      }

      private async Task<Guard> LockAsync_InternalAsync(CancellationToken cancellationToken) {
         if (alsLockDepth.Value == 1) {
            try {
               await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            } catch {
               // need to free our lock
               throw;
            }
         }
         return new Guard(this);
      }

      private void HandleUnlock() {
         alsLockDepth.Value--;

         if (alsLockDepth.Value == 0) {
            semaphore.Release();
         }
      }

      public class Guard : IDisposable {
         private const int kStateLockHeld = 0;
         private const int kStateLockFreed = 1;
         private const int kStateDisposed = 2;

         private AsyncLock parent;
         private int state = kStateLockHeld;

         public Guard(AsyncLock parent) {
            this.parent = parent;
         }

         public void Free() {
            var previous = Interlocked.CompareExchange(ref state, kStateLockHeld, kStateLockFreed);
            if (previous == kStateLockFreed) {
               throw new InvalidOperationException("Double Free Attempted!");
            } else if (previous == kStateDisposed) {
               throw new ObjectDisposedException(nameof(Guard), "Attempted to free lock guard after disposal.");
            }

            previous.AssertEquals(kStateLockHeld);
            parent.HandleUnlock();
         }

         public void Dispose() {
            var previous = Interlocked.CompareExchange(ref state, kStateLockHeld, kStateLockFreed);
            if (previous == kStateLockHeld) {
               parent.HandleUnlock();
            }
         }
      }
   }
}
