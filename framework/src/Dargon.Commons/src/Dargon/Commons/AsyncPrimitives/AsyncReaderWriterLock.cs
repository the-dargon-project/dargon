using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Dargon.Commons.AsyncPrimitives {
   public class AsyncReaderWriterLock {
      private readonly AsyncSemaphore semaphore = new AsyncSemaphore(1);
      private readonly ConcurrentQueue<AsyncLatch> readerQueue = new ConcurrentQueue<AsyncLatch>();
      private int pendingReadersCount = 0;
      private int readerCount = 0;

      public async Task<DecrementOnDisposeAndReleaseOnCallbackZeroResult> WriterLockAsync() {
         await semaphore.WaitAsync().ConfigureAwait(false);
         return new DecrementOnDisposeAndReleaseOnCallbackZeroResult(
            () => 0,
            semaphore);
      }

      public Task<DecrementOnDisposeAndReleaseOnCallbackZeroResult> ReaderLockAsync() {
         var spinner = new SpinWait();
         while (true) {
            var originalReaderCount = Interlocked.CompareExchange(ref readerCount, 0, 0);
            var nextReaderCount = originalReaderCount + 1;
            if (Interlocked.CompareExchange(ref readerCount, nextReaderCount, originalReaderCount) == originalReaderCount) {
               if (originalReaderCount == 0) {
                  return ReaderLockCoordinatorRoleAsync();
               } else {
                  return ReaderLockFollowerRoleAsync();
               }
            }
            spinner.SpinOnce();
         }
      }

      private async Task<DecrementOnDisposeAndReleaseOnCallbackZeroResult> ReaderLockCoordinatorRoleAsync() {
         await semaphore.WaitAsync().ConfigureAwait(false);

         int allReadersCount;
         while (true) {
            var readerCountCapture = readerCount;
            // allReadersCount += existingReaderCount;
            if (Interlocked.CompareExchange(ref readerCount, 0, readerCountCapture) == readerCountCapture) {
               allReadersCount = readerCountCapture;
               break;
            }
         }

         Interlocked.CompareExchange(ref pendingReadersCount, allReadersCount, 0).AssertEquals(0);

         var spinner = new SpinWait();
         for (var i = 0; i < allReadersCount - 1; i++) {
            AsyncLatch followerLatch;
            while (!readerQueue.TryDequeue(out followerLatch)) {
               spinner.SpinOnce();
            }
            followerLatch.SetOrThrow();
         }
         return new DecrementOnDisposeAndReleaseOnCallbackZeroResult(
            () => Interlocked.Decrement(ref pendingReadersCount),
            semaphore);
      }

      private async Task<DecrementOnDisposeAndReleaseOnCallbackZeroResult> ReaderLockFollowerRoleAsync() {
         var latch = new AsyncLatch();
         readerQueue.Enqueue(latch);
         await latch.WaitAsync(CancellationToken.None).ConfigureAwait(false);
         return new DecrementOnDisposeAndReleaseOnCallbackZeroResult(
            () => Interlocked.Decrement(ref pendingReadersCount),
            semaphore);
      }

      public struct DecrementOnDisposeAndReleaseOnCallbackZeroResult : IDisposable {
         private readonly Func<int> callback;
         private readonly AsyncSemaphore semaphore;

         public DecrementOnDisposeAndReleaseOnCallbackZeroResult(Func<int> callback, AsyncSemaphore semaphore) {
            this.callback = callback;
            this.semaphore = semaphore;
         }

         public void Dispose() {
            if (callback() == 0) {
               semaphore.Release();
            }
         }
      }
   }
}
