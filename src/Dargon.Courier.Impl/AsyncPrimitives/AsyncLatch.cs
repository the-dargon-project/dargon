using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;

namespace Dargon.Courier.AsyncPrimitives {
   /// <summary>
   /// Not-so-amazingly-performant (compared to a bool branch) variant
   /// of an awaitable once-latch that supports wait cancellation.
   /// </summary>
   public class AsyncLatch {
      private readonly AsyncSemaphore semaphore = new AsyncSemaphore(0);

      public void Set() {
         semaphore.Release(1337);
      }

      public async Task WaitAsync(CancellationToken token = default(CancellationToken)) {
         await semaphore.WaitAsync(token);
         semaphore.Release(1);
      }
   }
}