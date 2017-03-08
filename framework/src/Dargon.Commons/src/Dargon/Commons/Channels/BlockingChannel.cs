using Dargon.Commons.AsyncPrimitives;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons.Exceptions;

namespace Dargon.Commons.Channels {
   public class BlockingChannel<T> : Channel<T> {
      private readonly ConcurrentQueue<WriterContext> writerQueue = new ConcurrentQueue<WriterContext>();
      private readonly AsyncSemaphore queueSemaphore = new AsyncSemaphore(0);

      public int Count => queueSemaphore.Count;

      public async Task WriteAsync(T message, CancellationToken cancellationToken) {
         var context = new WriterContext(message);
         writerQueue.Enqueue(context);
         queueSemaphore.Release();
         try {
            await context.completionLatch.WaitAsync(cancellationToken).ConfigureAwait(false);
         } catch (OperationCanceledException) {
            while (true) {
               var originalValue = Interlocked.CompareExchange(ref context.state, WriterContext.kStateCancelled, WriterContext.kStatePending);
               if (originalValue == WriterContext.kStatePending) {
                  throw;
               } else if (originalValue == WriterContext.kStateCompleting) {
                  await context.completingFreedEvent.WaitAsync(CancellationToken.None).ConfigureAwait(false);
               } else if (originalValue == WriterContext.kStateCompleted) {
                  return;
               }
            }
         } finally {
            Assert.IsTrue(context.state == WriterContext.kStateCancelled ||
                          context.state == WriterContext.kStateCompleted);
         }
      }

      public bool TryRead(out T message) {
         if (!queueSemaphore.TryTake()) {
            message = default(T);
            return false;
         }
         SpinWait spinner = new SpinWait();
         WriterContext context;
         while (!writerQueue.TryDequeue(out context)) {
            spinner.SpinOnce();
         }
         var oldState = Interlocked.CompareExchange(ref context.state, WriterContext.kStateCompleting, WriterContext.kStatePending);
         if (oldState == WriterContext.kStatePending) {
            Interlocked.CompareExchange(ref context.state, WriterContext.kStateCompleted, WriterContext.kStateCompleting);
            context.completingFreedEvent.Set();
            context.completionLatch.SetOrThrow();
            message = context.value;
            return true;
         } else if (oldState == WriterContext.kStateCompleted) {
            throw new InvalidStateException();
         } else if (oldState == WriterContext.kStateCompleted) {
            throw new InvalidStateException();
         } else if (oldState == WriterContext.kStateCompleted) {
            message = default(T);
            return false;
         } else {
            throw new InvalidStateException();
         }
      }

      public async Task<T> ReadAsync(CancellationToken cancellationToken, Func<T, bool> acceptanceTest) {
         while (!cancellationToken.IsCancellationRequested) {
            await queueSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            WriterContext context;
            if (!writerQueue.TryDequeue(out context)) {
               throw new InvalidStateException();
            }
            var oldState = Interlocked.CompareExchange(ref context.state, WriterContext.kStateCompleting, WriterContext.kStatePending);
            if (oldState == WriterContext.kStatePending) {
               if (acceptanceTest(context.value)) {
                  Interlocked.CompareExchange(ref context.state, WriterContext.kStateCompleted, WriterContext.kStateCompleting);
                  context.completingFreedEvent.Set();
                  context.completionLatch.SetOrThrow();
                  return context.value;
               } else {
                  Interlocked.CompareExchange(ref context.state, WriterContext.kStatePending, WriterContext.kStateCompleting);
                  context.completingFreedEvent.Set();
                  writerQueue.Enqueue(context);
                  queueSemaphore.Release();
               }
            } else if (oldState == WriterContext.kStateCompleting) {
               throw new InvalidStateException();
            } else if (oldState == WriterContext.kStateCompleted) {
               throw new InvalidStateException();
            } else if (oldState == WriterContext.kStateCancelled) {
               continue;
            }
         }
         // throw is guaranteed
         cancellationToken.ThrowIfCancellationRequested();
         throw new InvalidStateException();
      }

      private class WriterContext {
         public const int kStatePending = 0;
         public const int kStateCompleting = 1;
         public const int kStateCompleted = 2;
         public const int kStateCancelled = 3;

         public readonly AsyncLatch completionLatch = new AsyncLatch();
         public readonly AsyncAutoResetLatch completingFreedEvent = new AsyncAutoResetLatch();
         public readonly T value;
         public int state = kStatePending;

         public WriterContext(T value) {
            this.value = value;
         } 
      }
   }
}