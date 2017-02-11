using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Courier;
using Dargon.Commons;
using Dargon.Commons.AsyncPrimitives;
using Dargon.Commons.Collections;
using Dargon.Hydrous.Impl.Store;
using SCG = System.Collections.Generic;

namespace Dargon.Hydrous.Impl.Store {
   public interface ICachePersistenceStrategy<K, V> : ICacheReadStrategy<K, V>, ICacheUpdateStrategy<K, V> { }

   public class CachePersistenceStrategy<K, V> : ICachePersistenceStrategy<K, V> {
      private readonly ICacheReadStrategy<K, V> readStrategy;
      private readonly ICacheUpdateStrategy<K, V> updateStrategy;

      private CachePersistenceStrategy(ICacheReadStrategy<K, V> readStrategy, ICacheUpdateStrategy<K, V> updateStrategy) {
         this.readStrategy = readStrategy;
         this.updateStrategy = updateStrategy;
      }

      public Task<Entry<K, V>> ReadAsync(K key) => readStrategy.ReadAsync(key);

      public Task HandleUpdateAsync(Entry<K, V> baseEntry, Entry<K, V> updatedEntry) => updateStrategy.HandleUpdateAsync(baseEntry, updatedEntry);

      public static ICachePersistenceStrategy<K, V> Create(ICacheReadStrategy<K, V> readStrategy, ICacheUpdateStrategy<K, V> updateStrategy) {
         return new CachePersistenceStrategy<K, V>(readStrategy, updateStrategy);
      }
   }

   public interface ICacheReadStrategy<K, V> {
      Task<Entry<K, V>> ReadAsync(K key);
   }

   public class DirectCacheReadStrategy<K, V> : ICacheReadStrategy<K, V> {
      private readonly IHitler<K, V> hitler;

      public DirectCacheReadStrategy(IHitler<K, V> hitler) {
         this.hitler = hitler;
      }

      public Task<Entry<K, V>> ReadAsync(K key) {
         return hitler.GetAsync(key);
      }
   }

   public class BatchedCacheReadStrategy<K, V> : ICacheReadStrategy<K, V> {
      private readonly ConcurrentQueue<Tuple<K, AsyncBox<Entry<K, V>>>> jobQueue = new ConcurrentQueue<Tuple<K, AsyncBox<Entry<K, V>>>>();
      private readonly AsyncSemaphore jobSignal = new AsyncSemaphore();
      private readonly IHitler<K, V> hitler;

      private BatchedCacheReadStrategy(IHitler<K, V> hitler) {
         this.hitler = hitler;
      }

      public void Initialize() {
         for (var i = 0; i < 4; i++) {
            RunAsync().Forget();
         }
      }

      private async Task RunAsync() {
         while (true) {
            await jobSignal.WaitAsync().ConfigureAwait(false);
            var jobResultBoxByKey = new SCG.Dictionary<K, AsyncBox<Entry<K, V>>>();
            var spinner = new SpinWait();
            do {
               Tuple<K, AsyncBox<Entry<K, V>>> job;
               while (!jobQueue.TryDequeue(out job)) {
                  spinner.SpinOnce();
               }
               jobResultBoxByKey.Add(job.Item1, job.Item2);
            } while (jobSignal.TryTake());

//            Console.WriteLine("Batch Read: " + jobResultBoxByKey.Count);
            var results = await hitler.GetManyAsync(new HashSet<K>(jobResultBoxByKey.Keys)).ConfigureAwait(false);
            foreach (var kvp in results) {
               jobResultBoxByKey[kvp.Key].SetResult(kvp.Value);
            }
         }
      }

      public Task<Entry<K, V>> ReadAsync(K key) {
         var box = new AsyncBox<Entry<K, V>>();
         jobQueue.Enqueue(Tuple.Create(key, box));
         jobSignal.Release();
         return box.GetResultAsync();
      }

      public static ICacheReadStrategy<K, V> Create(IHitler<K, V> hitler) {
         var result = new BatchedCacheReadStrategy<K, V>(hitler);
         result.Initialize();
         return result;
      }
   }

   public interface ICacheUpdateStrategy<K, V> {
      Task HandleUpdateAsync(Entry<K, V> baseEntry, Entry<K, V> updatedEntry);
   }

   public class WriteThroughCacheUpdateStrategy<K, V> : ICacheUpdateStrategy<K, V> {
      private readonly IHitler<K, V> hitler;

      public WriteThroughCacheUpdateStrategy(IHitler<K, V> hitler) {
         this.hitler = hitler;
      }

      public Task HandleUpdateAsync(Entry<K, V> baseEntry, Entry<K, V> updatedEntry) {
         return hitler.UpdateByDiffAsync(baseEntry, updatedEntry);
      }
   }
   
   public class WriteBehindCacheUpdateStrategy<K, V> : ICacheUpdateStrategy<K, V> {
      private readonly object synchronization = new object();
      private readonly IHitler<K, V> hitler;
      private readonly int batchUpdateIntervalMillis;
      private readonly ConcurrentQueue<PendingUpdate<K, V>> pendingUpdates = new ConcurrentQueue<PendingUpdate<K, V>>();
      private readonly AsyncSemaphore pendingUpdatesSignal = new AsyncSemaphore();

      private WriteBehindCacheUpdateStrategy(IHitler<K, V> hitler, int batchUpdateIntervalMillis) {
         this.hitler = hitler;
         this.batchUpdateIntervalMillis = batchUpdateIntervalMillis;
      }

      public void Initialize() {
         RunAsync().Forget();
      }

      private async Task RunAsync() {
         while (true) {
            Console.WriteLine("Begin BU");
            var sw = new Stopwatch();
            sw.Start();

            SCG.Dictionary<K, PendingUpdate<K, V>> mergedPendingUpdates = new SCG.Dictionary<K, PendingUpdate<K, V>>();
            var spinner = new SpinWait();
            while (pendingUpdatesSignal.TryTake()) {
               PendingUpdate<K, V> pendingUpdate;
               while (!pendingUpdates.TryDequeue(out pendingUpdate)) {
                  spinner.SpinOnce();
               }
               PendingUpdate<K, V> existingPendingUpdate;
               if (mergedPendingUpdates.TryGetValue(pendingUpdate.Base.Key, out existingPendingUpdate)) {
                  existingPendingUpdate.Updated = pendingUpdate.Updated;
               } else {
                  mergedPendingUpdates[pendingUpdate.Base.Key] = pendingUpdate;
               }
            }

            if (mergedPendingUpdates.Any()) {
               await hitler.BatchUpdateAsync(mergedPendingUpdates.Values.ToArray()).ConfigureAwait(false);
            }
            Console.WriteLine("END BU " + sw.ElapsedMilliseconds);
            await Task.Delay(batchUpdateIntervalMillis).ConfigureAwait(false);
         }
      }

      public Task HandleUpdateAsync(Entry<K, V> baseEntry, Entry<K, V> updatedEntry) {
         pendingUpdates.Enqueue(
            new PendingUpdate<K, V> {
               Base = baseEntry,
               Updated = updatedEntry
            });
         pendingUpdatesSignal.Release();
         return Task.CompletedTask;
      }

      public static WriteBehindCacheUpdateStrategy<K, V> Create(IHitler<K, V> hitler, int batchUpdateIntervalMillis = 30000) {
         var result = new WriteBehindCacheUpdateStrategy<K, V>(hitler, batchUpdateIntervalMillis);
         result.Initialize();
         return result;
      }
   }
}