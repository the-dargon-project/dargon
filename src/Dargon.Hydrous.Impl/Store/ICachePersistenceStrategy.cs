using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Dargon.Courier;
using Dargon.Commons;

namespace Dargon.Hydrous.Impl.Store {
   public interface ICachePersistenceStrategy<K, V> {
      Task<Entry<K, V>> ReadAsync(K key);
      Task HandleUpdateAsync(Entry<K, V> baseEntry, Entry<K, V> updatedEntry);
   }

   public class WriteThroughCachePersistenceStrategy<K, V> : ICachePersistenceStrategy<K, V> {
      private readonly IHitler<K, V> hitler;

      public WriteThroughCachePersistenceStrategy(IHitler<K, V> hitler) {
         this.hitler = hitler;
      }

      public Task<Entry<K, V>> ReadAsync(K key) {
         return hitler.GetAsync(key);
      }

      public Task HandleUpdateAsync(Entry<K, V> baseEntry, Entry<K, V> updatedEntry) {
         return hitler.UpdateByDiffAsync(baseEntry, updatedEntry);
      }
   }
   
   public class WriteBehindCachePersistenceStrategy<K, V> : ICachePersistenceStrategy<K, V> {
      private readonly object synchronization = new object();
      private readonly IHitler<K, V> hitler;
      private readonly int batchUpdateIntervalMillis;
      private Dictionary<K, PendingUpdate<K, V>> pendingUpdatesByKey = new Dictionary<K, PendingUpdate<K, V>>();

      private WriteBehindCachePersistenceStrategy(IHitler<K, V> hitler, int batchUpdateIntervalMillis) {
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
            await hitler.BatchUpdateAsync(TakeAndSwapPendingUpdates().Values.ToArray()).ConfigureAwait(false);
//            var pendingUpdates = TakeAndSwapPendingUpdates();
//            if (pendingUpdates.Any()) {
//               await hitler.BatchUpdateAsync(pendingUpdates.Values.ToArray()).ConfigureAwait(false);
//            }
            Console.WriteLine("END BU " + sw.ElapsedMilliseconds);
            await Task.Delay(batchUpdateIntervalMillis).ConfigureAwait(false);
         }
      }

      private Dictionary<K, PendingUpdate<K, V>> TakeAndSwapPendingUpdates() {
         lock (synchronization) {
            var old = pendingUpdatesByKey;
            pendingUpdatesByKey = new Dictionary<K, PendingUpdate<K, V>>();
            return old;
         }
      }

      public Task<Entry<K, V>> ReadAsync(K key) {
         return hitler.GetAsync(key);
      }

      public Task HandleUpdateAsync(Entry<K, V> baseEntry, Entry<K, V> updatedEntry) {
         lock (synchronization) {
            PendingUpdate<K, V> existingPendingUpdate;
            if (pendingUpdatesByKey.TryGetValue(baseEntry.Key, out existingPendingUpdate)) {
               existingPendingUpdate.Updated = updatedEntry;
            } else {
               pendingUpdatesByKey[baseEntry.Key] = new PendingUpdate<K, V> {
                  Base = baseEntry,
                  Updated = updatedEntry
               };
            }
         }
         return Task.CompletedTask;
      }

      public static WriteBehindCachePersistenceStrategy<K, V> Create(IHitler<K, V> hitler, int batchUpdateIntervalMillis = 30000) {
         var result = new WriteBehindCachePersistenceStrategy<K, V>(hitler, batchUpdateIntervalMillis);
         result.Initialize();
         return result;
      }
   }
}