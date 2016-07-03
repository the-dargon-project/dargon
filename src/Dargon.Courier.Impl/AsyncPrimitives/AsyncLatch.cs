using Nito.AsyncEx;
using System.Threading;
using System.Threading.Tasks;

namespace Dargon.Courier.AsyncPrimitives {
   /// <summary>
   /// Not-so-amazingly-performant (compared to a bool branch) variant
   /// of an awaitable once-latch that supports wait cancellation.
   /// </summary>
   public class AsyncLatch {
      private readonly TaskCompletionSource tcs = new TaskCompletionSource();
      private const int kStateUnsignalled = 0;
      private const int kStateSignalled = 1;
      private int state = kStateUnsignalled;

      public Task WaitAsync(CancellationToken token = default(CancellationToken)) {
         return Task.WhenAny(
            token.AsTask(),
            tcs.Task);
      }

      public void Set() {
         if (Interlocked.CompareExchange(ref state, kStateSignalled, kStateUnsignalled) == kStateUnsignalled) {
            tcs.SetResult();
         }
      }
   }
}