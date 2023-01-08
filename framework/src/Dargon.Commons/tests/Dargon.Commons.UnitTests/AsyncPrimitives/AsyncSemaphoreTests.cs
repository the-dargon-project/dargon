using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NMockito;
using Xunit;

namespace Dargon.Commons.AsyncPrimitives {
   public class AsyncSemaphoreTests : NMockitoInstance {
      [Fact]
      public async Task HappyPathTest() {
         var semaphore = new AsyncSemaphore(2);
         await semaphore.WaitAsync();
         await semaphore.WaitAsync();

         try {
            semaphore.WaitAsync(new CancellationTokenSource(1000).Token).Wait();
            throw new Exception("Expected an OpCanceledException?");
         } catch (AggregateException ae) when (ae.InnerExceptions[0] is OperationCanceledException) {
         }

         semaphore.Release();
         await semaphore.WaitAsync();
      }
   }
}
