using Dargon.Commons;
using NMockito;
using System;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons.AsyncPrimitives;
using Xunit;
using static Dargon.Commons.Channels.ChannelsExtensions;

namespace Dargon.Hydrous {
   public class PutFT : NMockitoInstance {
      [Fact]
      public async Task PutTestAsync() {
         await TaskEx.YieldToThreadPool();

         ThreadPool.SetMaxThreads(128, 128);

         var cohortCount = 4;
         var cluster = await TestUtils.CreateCluster<int, string>(cohortCount).ConfigureAwait(false);

         var workerCount = 100;
         var sync = new AsyncCountdownLatch(workerCount);
         var tasks = Util.Generate(
            workerCount,
            key => Go(async () => {
               try {
                  Console.WriteLine("Entered thread for worker of key " + key);

                  sync.Signal();
                  await sync.WaitAsync().ConfigureAwait(false);

                  const int kWriteCount = 10;
                  for (var i = 0; i < kWriteCount; i++) {
                     var node = cluster[i % cohortCount];
                     var previousEntry = await node.UserCache.PutAsync(key, "value" + i).ConfigureAwait(false);
                     AssertEquals(key, previousEntry.Key);
                     if (i == 0) {
                        AssertNull(previousEntry.Value);
                        AssertFalse(previousEntry.Exists);
                     } else {
                        AssertEquals("value" + (i - 1), previousEntry.Value);
                        AssertTrue(previousEntry.Exists);
                     }
                  }
                  {
                     var node = cluster[kWriteCount % cohortCount];
                     var currentEntry = await node.UserCache.GetAsync(key).ConfigureAwait(false);
                     AssertEquals(key, currentEntry.Key);
                     AssertEquals("value" + (kWriteCount - 1), currentEntry.Value);
                     AssertEquals(true, currentEntry.Exists);
                  }
               } catch (Exception e) {
                  Console.Error.WriteLine("Worker on key " + key + " threw " + e);
                  throw;
               }
            }));
         try {
            await Task.WhenAll(tasks).ConfigureAwait(false);
         } catch (Exception e) {
            Console.Error.WriteLine("Test threw " + e);
            throw;
         }
      }
   }
}