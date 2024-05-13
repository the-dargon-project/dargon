// #define ENABLE_LOCK_STACK_CAPTURE

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Dargon.Commons.AsyncPrimitives {
   /// <summary>
   /// Entering the first read or write requires taking the semaphore.
   /// </summary>
   public class AsyncReaderWriterLock {
      private readonly AsyncSemaphore semaphore = new AsyncSemaphore(1);
      private DebugStackCapture debugStackCapture = new();
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
      public Task<AsyncGuard> CreateWriterGuardAsync() {
         if (alsReaderDepth.Value != 0) {
            throw new InvalidOperationException($"Cannot access async writer lock while under a reader lock.");
         }

         // If the local async control flow has not yet acquired a writer
         // lock, then take exclusive access via semaphore. 
         alsWriterDepth.Value++;

         return CreateWriterGuardAsync_InternalAsync();
      }

      private async Task<AsyncGuard> CreateWriterGuardAsync_InternalAsync() {
         if (alsWriterDepth.Value == 1) {
            await semaphore.WaitAsync().ConfigureAwait(false);
            debugStackCapture.Set();
         }

         return new AsyncGuard(this, true);
      }

      private ValueTask HandleFreeWriterGuardAsync() {
         alsWriterDepth.Value--;
         alsWriterDepth.Value.AssertIsGreaterThanOrEqualTo(0);

         if (alsWriterDepth.Value == 0) {
            debugStackCapture.Zero();
            semaphore.Release();
         }

         return ValueTask.CompletedTask;
      }

      public SyncGuard CreateWriterGuard() {
         alsReaderDepth.Value.AssertEquals(0);
         alsWriterDepth.Value++;
         if (alsWriterDepth.Value == 1) {
            semaphore.Wait();
            debugStackCapture.Set();
         }
         return new SyncGuard(this, true);
      }

      private void HandleFreeWriterGuard() {
         alsWriterDepth.Value--;
         alsWriterDepth.Value.AssertIsGreaterThanOrEqualTo(0);
         if (alsWriterDepth.Value == 0) {
            debugStackCapture.Zero();
            semaphore.Release();
         }
      }

      // See important notice in CreateWriterGuardAsync on why this method
      // cannot be marked as `async`!!
      public Task<AsyncGuard> CreateReaderGuardAsync() {
         if (alsWriterDepth.Value != 0) {
            return CreateWriterGuardAsync();
         }

         alsReaderDepth.Value++;
         return CreateReaderGuardAsync_InternalAsync();
      }

      private async Task<AsyncGuard> CreateReaderGuardAsync_InternalAsync() {
         if (alsReaderDepth.Value == 1) {
            using var guard = await readersLock.LockAsync();
            readerCount++;

            var rgs = readersGoSignal;
            if (rgs == null) {
               rgs = readersGoSignal = new();
               readerCount.AssertEquals(1);
               guard.Dispose();

               await semaphore.WaitAsync();
               debugStackCapture.Set();
               rgs.SetOrThrow();
            } else {
               guard.Dispose();
               await rgs.WaitAsync();
            }
         }
         return new AsyncGuard(this, false);
      }

      public SyncGuard CreateReaderGuard() {
         if (alsWriterDepth.Value != 0) {
            return CreateWriterGuard();
         }

         alsReaderDepth.Value++;
         if (alsReaderDepth.Value == 1) {
            using var guard = readersLock.Lock();
            readerCount++;

            var rgs = readersGoSignal;
            if (rgs == null) {
               rgs = readersGoSignal = new();
               readerCount.AssertEquals(1);
               guard.Dispose();

               semaphore.Wait();
               debugStackCapture.Set();
               rgs.SetOrThrow();
            } else {
               guard.Dispose();
               rgs.Wait();
            }
         }

         return new SyncGuard(this, false);
      }

      // See important notice in CreateWriterGuardAsync on why this method
      // cannot be marked as `async`!!
      private ValueTask HandleFreeReaderGuardAsync() {
         alsReaderDepth.Value--;
         alsReaderDepth.Value.AssertIsGreaterThanOrEqualTo(0);

         if (alsReaderDepth.Value != 0) {
            return ValueTask.CompletedTask;
         }

         return HandleFreeReaderGuardAsync_InternalAsync();
      }

      private async ValueTask HandleFreeReaderGuardAsync_InternalAsync() {
         using var guard = await readersLock.LockAsync();
         readerCount--;
         readerCount.AssertIsGreaterThanOrEqualTo(0);

         if (readerCount == 0) {
            readersGoSignal = null;
            guard.Dispose();
            debugStackCapture.Zero();
            semaphore.Release();
         }
      }

      private void HandleFreeReaderGuard() {
         alsReaderDepth.Value--;
         alsReaderDepth.Value.AssertIsGreaterThanOrEqualTo(0);
         if (alsReaderDepth.Value != 0) return;
         using var guard = readersLock.Lock();
         readerCount--;
         readerCount.AssertIsGreaterThanOrEqualTo(0);
         if (readerCount == 0) {
            readersGoSignal = null;
            guard.Dispose();
            debugStackCapture.Zero();
            semaphore.Release();
         }
      }

      private struct DebugStackCapture {
         private StackTrace inner;
         private string str;

         public void Set() {
#if DEBUG && ENABLE_LOCK_STACK_CAPTURE
            inner = new();
            str = inner.ToString();
#endif
         }

         public void Zero() {
#if DEBUG && ENABLE_LOCK_STACK_CAPTURE
            inner = null;
#endif
         }

         public override string ToString() => str;
      }

      public struct AsyncGuard : IAsyncDisposable {
         private readonly AsyncReaderWriterLock parent;
         private readonly bool isWriterElseReader;
         private AtomicLatch disposeLatch = new();

         public AsyncGuard(AsyncReaderWriterLock parent, bool isWriterElseReader) {
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

      public struct SyncGuard : IDisposable {
         private readonly AsyncReaderWriterLock parent;
         private readonly bool isWriterElseReader;
         private AtomicLatch disposeLatch = new();

         public SyncGuard(AsyncReaderWriterLock parent, bool isWriterElseReader) {
            this.parent = parent;
            this.isWriterElseReader = isWriterElseReader;
         }

         public void Dispose() {
            if (!disposeLatch.TrySetOnce()) return;

            if (isWriterElseReader) {
               parent.HandleFreeWriterGuard();
            } else {
               parent.HandleFreeReaderGuard();
            }
         }
      }
   }
}
