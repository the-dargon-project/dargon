using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Dargon.Commons.AsyncPrimitives {
   /// <summary>
   /// Entering the first read or write requires taking the semaphore.
   /// </summary>
   public class AsyncReaderWriterLock {
      private readonly AsyncSemaphore semaphore = new AsyncSemaphore(1); 
      private readonly AsyncLocal<int> alsWriterDepth = new();

      private readonly AsyncLock readersLock = new();
      private readonly AsyncLocal<int> alsReaderDepth = new();
      private AsyncLatch readersGoSignal;
      private int readerCount = 0;

      public int DebugWriterDepth => alsWriterDepth.Value;
      public int DebugReaderDepth => alsReaderDepth.Value;
      public (int reader, int writer) DebugReaderWriterDepth => (DebugReaderDepth, DebugWriterDepth);

      // IMPORTANT: This method is not declared `async`. Within an `async` method,
      // setting an AsyncLocal creates a new ExecutionContext with 'ambient' state
      // propagated to further `async` calls. We want the Reader/Writer Depths to
      // persist at the caller's scope, so we don't declare the method as async.
      public Task<Guard> CreateWriterGuardAsync() {
         if (alsReaderDepth.Value != 0) {
            throw new InvalidOperationException($"Cannot access async writer lock while under a reader lock.");
         }

         // If the local async control flow has not yet acquired a writer
         // lock, then take exclusive access via semaphore. 
         alsWriterDepth.Value++;

         return CreateWriterGuardAsync_InternalAsync();
      }

      private async Task<Guard> CreateWriterGuardAsync_InternalAsync() {
         if (alsWriterDepth.Value == 1) {
            await semaphore.WaitAsync().ConfigureAwait(false);
         }

         return new Guard(this, true);
      }

      private ValueTask HandleFreeWriterGuardAsync() {
         alsWriterDepth.Value--;

         if (alsWriterDepth.Value == 0) {
            semaphore.Release();
         }

         return ValueTask.CompletedTask;
      }

      // See important notice in CreateWriterGuardAsync on why this method
      // cannot be marked as `async`!!
      public Task<Guard> CreateReaderGuardAsync() {
         if (alsWriterDepth.Value != 0) {
            return CreateWriterGuardAsync();
         }

         alsReaderDepth.Value++;
         return CreateReaderGuardAsync_InternalAsync();
      }

      private async Task<Guard> CreateReaderGuardAsync_InternalAsync() {
         if (alsReaderDepth.Value == 0) {
            using var guard = await readersLock.LockAsync();
            readerCount++;

            var rgs = readersGoSignal;
            if (rgs == null) {
               rgs = readersGoSignal = new();
               readerCount.AssertEquals(1);
               guard.Dispose();

               await semaphore.WaitAsync();
               rgs.SetOrThrow();
            } else {
               guard.Dispose();
               await rgs.WaitAsync();
            }
         }
         return new Guard(this, false);
      }

      // See important notice in CreateWriterGuardAsync on why this method
      // cannot be marked as `async`!!
      private ValueTask HandleFreeReaderGuardAsync() {
         alsReaderDepth.Value--;

         if (alsReaderDepth.Value != 0) {
            return ValueTask.CompletedTask;
         }

         return HandleFreeReaderGuardAsync_InternalAsync();
      }

      private async ValueTask HandleFreeReaderGuardAsync_InternalAsync() {
         using var guard = await readersLock.LockAsync();
         readerCount--;

         if (readerCount == 0) {
            readersGoSignal = null;
            guard.Dispose();

            semaphore.Release();
         }
      }

      public struct Guard : IAsyncDisposable {
         private readonly AsyncReaderWriterLock parent;
         private readonly bool isWriterElseReader;
         private AtomicLatch disposeLatch = new();

         public Guard(AsyncReaderWriterLock parent, bool isWriterElseReader) {
            this.parent = parent;
            this.isWriterElseReader = isWriterElseReader;
         }

         public ValueTask DisposeAsync() {
            if (!disposeLatch.TrySetOnce()) return ValueTask.CompletedTask;

            if (isWriterElseReader) {
               return parent.HandleFreeWriterGuardAsync();
            } else {
               return parent.HandleFreeReaderGuardAsync();
            }
         }
      }
   }
}
