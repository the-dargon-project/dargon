using System;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons.AsyncPrimitives;
using NMockito;
using NMockito.Attributes;
using Xunit;
using static Dargon.Commons.Channels.ChannelsExtensions;

namespace Dargon.Commons.Pooling {
   public class AsyncLocalBufferManagerTests : NMockitoInstance {
      [Fact]
      public async Task Run() {
         var bufferManager = new AsyncLocalBufferManager<int>(i => Arrays.Create(i, j => j % 2), 2);
         var nparallelism = 3;
         var barrier = new AsyncBarrier(nparallelism);
         var completionBarrier = new AsyncBarrier(nparallelism + 1); // +1 for test driver task
         for (var i = 0; i < nparallelism; i++) {
            var cohort = i; // value capture
            Go(async () => {
               for (var j = 0; j < 20; j++) {
                  // take buf0, buf1
                  var buf0 = bufferManager.Take(0);
                  AssertEquals(buf0.Length, 0);
                  var buf1 = bufferManager.Take(0);
                  AssertEquals(buf1.Length, 0);
                  AssertNotEquals(buf0, buf1);

                  // can't take beyond reentrancy
                  AssertThrows<InvalidOperationException>(() => bufferManager.Take(0));
                  
                  // can't return wrong buffer (end)
                  AssertThrows<InvalidOperationException>(() => bufferManager.Give(buf0));

                  // can't return buffer twice.
                  bufferManager.Give(buf1);
                  AssertThrows<InvalidOperationException>(() => bufferManager.Give(buf1));

                  // getting yields same buffer
                  AssertEquals(buf1, bufferManager.Take(0));
                  bufferManager.Give(buf1);
                  bufferManager.Give(buf0);

                  // can't give same buffer twice.
                  AssertThrows<InvalidOperationException>(() => bufferManager.Give(buf0));
                  await barrier.SignalAndWaitAsync();
               }

               // test asynclocal resizes independently.
               for (var j = 0; j < 20; j++) {
                  if (cohort == 0) {
                     var buf0 = bufferManager.Take(61);
                     var buf1 = bufferManager.Take(123);
                     AssertEquals(64, buf0.Length);
                     AssertEquals(128, buf1.Length);
                     bufferManager.Give(buf1);
                     bufferManager.Give(buf0);
                  } else {
                     var buf0 = bufferManager.Take(4);
                     var buf1 = bufferManager.Take(4);
                     AssertEquals(4, buf0.Length);
                     AssertEquals(4, buf1.Length);
                     bufferManager.Give(buf1);
                     bufferManager.Give(buf0);
                  }
                  await barrier.SignalAndWaitAsync();
               }
               await completionBarrier.SignalAndWaitAsync();
            }).Forget();
         }
         await completionBarrier.SignalAndWaitAsync();

         // Ensure new round of tasks gets its own buffers.
         for (var i = 0; i < nparallelism; i++) {
            Go(async () => {
               for (var j = 0; j < 20; j++) {
                  var buf0 = bufferManager.Take(2);
                  var buf1 = bufferManager.Take(2);
                  AssertEquals(2, buf0.Length);
                  AssertEquals(2, buf1.Length);
                  bufferManager.Give(buf1);
                  bufferManager.Give(buf0);
                  await barrier.SignalAndWaitAsync();
               }
               await completionBarrier.SignalAndWaitAsync();
            }).Forget();
         }
         await completionBarrier.SignalAndWaitAsync();
      }
   }
}
