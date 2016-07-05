using Dargon.Commons;
using Dargon.Commons.AsyncPrimitives;
using static Dargon.Commons.Channels.ChannelsExtensions;
using Dargon.Courier;
using Dargon.Hydrous.Impl;
using Dargon.Hydrous.Impl.Store;
using Dargon.Hydrous.Impl.Store.Postgre;
using Dargon.Vox;
using NMockito;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SCG = System.Collections.Generic;

namespace Dargon.Hydrous.Cache {
   public class WriteBehindFT : NMockitoInstance {
      private const int kRowCount = 5000;
      private readonly IHitler<int, TestDto> hitler = new PostgresHitler<int, TestDto>("test", StaticTestConfiguration.PostgreConnectionString);
      private readonly SCG.Dictionary<string, int> entryIdsByOriginalName = new SCG.Dictionary<string, int>();

      public async Task SetupAsync() {
         await CleanupAsync().ConfigureAwait(false);

         for (var i = 0; i < kRowCount; i++) {
            var entryName = "Name" + i;
            var entry = await hitler.InsertAsync(new TestDto { Name = entryName }).ConfigureAwait(false);
            entryIdsByOriginalName.Add(entryName, entry.Key);
         }
      }

      public async Task CleanupAsync() {
         await hitler.ClearAsync().ConfigureAwait(false);
      }

      public async Task RunAsync() {
         await TaskEx.YieldToThreadPool();
//         ThreadPool.SetMaxThreads(128, 128);

         await SetupAsync().ConfigureAwait(false);

         var clusterSize = 4;
         var cluster = await TestUtils.CreateCluster<int, TestDto>(
            clusterSize,
            () => new CacheConfiguration<int, TestDto>("test-cache") {
               CachePersistenceStrategy = new WriteBehindCachePersistenceStrategy<int, TestDto>(hitler),
               PartitioningConfiguration = new PartitioningConfiguration {
                  Redundancy = 2
               }
            }).ConfigureAwait(false);

         var workerCount = 4;
         var sync = new AsyncCountdownLatch(workerCount);
         var tasks = Util.Generate(
            workerCount,
            async workerId => {
               await TaskEx.YieldToThreadPool();

               sync.Signal();
               await sync.WaitAsync().ConfigureAwait(false);

               var jobs = Util.Generate(
                  kRowCount,
                  row => cluster[(workerId + row) % clusterSize].UserCache.ProcessAsync(
                     entryIdsByOriginalName["Name" + row],
                     AppendToNameOperation.Create("_")
                     ));
               await Task.WhenAll(jobs).ConfigureAwait(false);
            });

         try {
            await Task.WhenAll(tasks).ConfigureAwait(false);

            Console.WriteLine("Validating cache state");

            await Task.WhenAll(
               Util.Generate(
                  kRowCount,
                  i => Go(async () => {
                     var originalName = "Name" + i;
                     var entryId = entryIdsByOriginalName[originalName];
                     var entry = await cluster[i % clusterSize].UserCache.GetAsync(entryId).ConfigureAwait(false);
                     AssertEquals(originalName + "_".Repeat(clusterSize), entry.Value.Name);
                  }))).ConfigureAwait(false);

            await CleanupAsync().ConfigureAwait(false);
         } catch (Exception e) {
            Console.WriteLine("Write behind test threw " + e);
            throw;
         }
      }

      [AutoSerializable]
      public class TestDto {
         public string Name { get; set; }
         public DateTime Created { get; set; }
         public DateTime Updated { get; set; }
      }

      [AutoSerializable]
      public class AppendToNameOperation : IEntryOperation<int, TestDto, bool> {
         public EntryOperationType Type => EntryOperationType.ConditionalUpdate;

         public Guid Id { get; set; }
         public string What { get; set; }

         public Task<bool> ExecuteAsync(Entry<int, TestDto> entry) {
            if (!entry.Exists) {
               return Task.FromResult(false);
            }
            entry.Value.Name += "_";
            entry.IsDirty = true;
            return Task.FromResult(true);
         }

         public static AppendToNameOperation Create(string what) => new AppendToNameOperation {
            Id = Guid.NewGuid(),
            What = what
         };
      }
   }
}
