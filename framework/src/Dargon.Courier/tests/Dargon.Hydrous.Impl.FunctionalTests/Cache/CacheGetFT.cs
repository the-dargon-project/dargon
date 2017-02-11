using Dargon.Commons;
using Dargon.Commons.Channels;
using NMockito;
using System;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons.AsyncPrimitives;
using Xunit;

namespace Dargon.Hydrous {
   public class CacheGetFT : NMockitoInstance {
      [Fact]
      public async Task RunAsync() {
         await TaskEx.YieldToThreadPool();

         ThreadPool.SetMaxThreads(128, 128);

         var cohortCount = 4;
         var cluster = await TestUtils.CreateCluster<int, string>(cohortCount).ConfigureAwait(false);

         var workerCount = 100;
         var sync = new AsyncCountdownLatch(workerCount);
         var tasks = Util.Generate(
            workerCount,
            key => ChannelsExtensions.Go(async () => {
               try {
                  Console.WriteLine("Entered thread for worker of key " + key);

                  sync.Signal();
                  await sync.WaitAsync().ConfigureAwait(false);

                  const int kReadCount = 10;
                  for (var i = 0; i < kReadCount; i++) {
                     var node = cluster[i % cohortCount];
                     var entry = await node.UserCache.GetAsync(key).ConfigureAwait(false);
                     AssertEquals(key, entry.Key);
                     AssertEquals(null, entry.Value);
                     AssertFalse(entry.Exists);
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