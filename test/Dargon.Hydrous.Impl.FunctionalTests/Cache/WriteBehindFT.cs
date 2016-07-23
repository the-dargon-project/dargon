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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SCG = System.Collections.Generic;

namespace Dargon.Hydrous.Cache {
   public abstract class WriteBehindFTBase : NMockitoInstance {
      private readonly IHitler<int, TestDto> hitler = new PostgresHitler<int, TestDto>("test", StaticTestConfiguration.PostgreConnectionString);
      private readonly SCG.Dictionary<string, int> entryIdsByOriginalName = new SCG.Dictionary<string, int>();
      private readonly int rowCount;
      private readonly int clusterSize;
      private readonly int replicationFactor;
      private readonly int workerCount;
      private readonly int iterationsPerWorker;

      protected WriteBehindFTBase(int rowCount, int clusterSize, int replicationFactor, int workerCount, int iterationsPerWorker) {
         this.rowCount = rowCount;
         this.clusterSize = clusterSize;
         this.replicationFactor = replicationFactor;
         this.workerCount = workerCount;
         this.iterationsPerWorker = iterationsPerWorker;
      }

      public async Task SetupAsync() {
         await CleanupAsync().ConfigureAwait(false);

         for (var i = 0; i < rowCount; i++) {
            var entryName = "Name" + i;
            var entry = await hitler.InsertAsync(new TestDto { Name = entryName }).ConfigureAwait(false);
            entryIdsByOriginalName.Add(entryName, entry.Key);
         }
      }

      public async Task CleanupAsync() {
         await hitler.ClearAsync().ConfigureAwait(false);
      }

      public async Task RunAsync() {
         int minWorkerThreads, maxWorkerThreads, minCompletionPortThreads, maxCompletionPortThreads;
         ThreadPool.GetMinThreads(out minWorkerThreads, out minCompletionPortThreads);
         ThreadPool.GetMaxThreads(out maxWorkerThreads, out maxCompletionPortThreads);
         Console.WriteLine($"Test Configuration: workers {minWorkerThreads}/{maxWorkerThreads}, iocp {minCompletionPortThreads}/{maxCompletionPortThreads}");

         await TaskEx.YieldToThreadPool();
//         ThreadPool.SetMaxThreads(128, 128);

         await SetupAsync().ConfigureAwait(false);

         var sw = new Stopwatch();
         sw.Start();

         Console.WriteLine(sw.ElapsedMilliseconds + " Starting Cluster");

         var cluster = await TestUtils.CreateCluster<int, TestDto>(
            clusterSize,
            () => new CacheConfiguration<int, TestDto>("test-cache") {
//               CachePersistenceStrategy = CachePersistenceStrategy<int, TestDto>.Create(
//                  BatchedCacheReadStrategy<int, TestDto>.Create(hitler),
//                  WriteBehindCacheUpdateStrategy<int, TestDto>.Create(hitler, 5000)),
               PartitioningConfiguration = new PartitioningConfiguration {
                  Redundancy = replicationFactor
               }
            }).ConfigureAwait(false);

         Console.WriteLine(sw.ElapsedMilliseconds + " Started Cluster");

         var sync = new AsyncCountdownLatch(workerCount);
         var tasks = Util.Generate(
            workerCount,
            async workerId => {
               await TaskEx.YieldToThreadPool();

               sync.Signal();
               await sync.WaitAsync().ConfigureAwait(false);

               var jobs = Util.Generate(
                  rowCount * iterationsPerWorker,
                  i => cluster[(workerId + i) % clusterSize].UserCache.ProcessAsync(
                     entryIdsByOriginalName["Name" + (i % rowCount)],
                     AppendToNameOperation.Create("_")
                     ));
               Console.WriteLine(DateTime.Now + " Worker " + workerId + " started iterations");
               await Task.WhenAll(jobs).ConfigureAwait(false);
               Console.WriteLine(DateTime.Now + " Worker " + workerId + " completed iterations");
            });

         try {
            Console.WriteLine(sw.ElapsedMilliseconds + " Awaiting workers");
            await Task.WhenAll(tasks).ConfigureAwait(false);

            Console.WriteLine(sw.ElapsedMilliseconds + " Validating cache state");

            await Task.WhenAll(
               Util.Generate(
                  rowCount,
                  i => Go(async () => {
                     var originalName = "Name" + i;
                     var entryId = entryIdsByOriginalName[originalName];
                     var entry = await cluster[i % clusterSize].UserCache.GetAsync(entryId).ConfigureAwait(false);
                     AssertEquals(originalName + "_".Repeat(workerCount * iterationsPerWorker), entry.Value.Name);
                  }))).ConfigureAwait(false);

            Console.WriteLine(sw.ElapsedMilliseconds + " Validation completed");

            await CleanupAsync().ConfigureAwait(false);
            while (true) ;
         } catch (Exception e) {
            Console.WriteLine("Write behind test threw " + e);
            throw;
         }
      }

      [AutoSerializable]
      public class TestDto {
         [RequiredColumn] public string Name { get; set; }
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

   public class SingleNodeSingleWorkerWriteBehindFT : WriteBehindFTBase {
      public SingleNodeSingleWorkerWriteBehindFT() : base(20000, 1, 1, 1, 20) { }
   }

   public class MultipleNodeMultipleWorkerWriteBehindFT : WriteBehindFTBase {
      public MultipleNodeMultipleWorkerWriteBehindFT() : base(20000, 4, 2, 4, 1) { }
   }
}
