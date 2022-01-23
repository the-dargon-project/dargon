using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dargon.Commons.AsyncPrimitives {
   /// <summary>
   /// Not-so-amazingly-performant (compared to a bool branch) variant
   /// of an awaitable once-latch that supports wait cancellation.
   /// </summary>
   public class AsyncLatch {
      private readonly TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
      private const int kStateUnsignalled = 0;
      private const int kStateSignalled = 1;
      private int state = kStateUnsignalled;

      public bool IsSignalled => Interlocked2.Read(ref state) == kStateSignalled;

      public Task WaitAsync(CancellationToken token = default(CancellationToken)) {
         if (!token.CanBeCanceled) {
            return tcs.Task;
         } else {
            var innerTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            token.Register(CancelCallback, innerTcs, false);
            var forget = tcs.Task.ContinueWith(DoneCallback, innerTcs, token);
            return innerTcs.Task;
         }
      }

      private static void DoneCallback(Task t, object x) {
         ((TaskCompletionSource<bool>)x).TrySetResult(false);
      }

      private static void CancelCallback(object x) {
         ((TaskCompletionSource<bool>)x).TrySetException(new TaskCanceledException());
      }

      public bool TrySet() {
         if (Interlocked.CompareExchange(ref state, kStateSignalled, kStateUnsignalled) == kStateUnsignalled) {
            tcs.SetResult(false);
            return true;
         }
         return false;
      }

      public void SetOrThrow() {
         if (!TrySet()) {
            throw new InvalidOperationException("Latch was already in set state.");
         }
      }
   }
}