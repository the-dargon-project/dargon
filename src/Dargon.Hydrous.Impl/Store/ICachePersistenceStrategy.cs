using System.Threading.Tasks;
using Dargon.Courier;

namespace Dargon.Hydrous.Impl.Store {
   public interface ICachePersistenceStrategy<K, V> {
      Task<Entry<K, V>> ReadAsync(K key);
      Task HandleUpdateAsync(Entry<K, V> baseEntry, Entry<K, V> updatedEntry);
   }

   public class WriteBehindCachePersistenceStrategy<K, V> : ICachePersistenceStrategy<K, V> {
      private readonly IHitler<K, V> hitler;

      public WriteBehindCachePersistenceStrategy(IHitler<K, V> hitler) {
         this.hitler = hitler;
      }

      public Task<Entry<K, V>> ReadAsync(K key) {
         return hitler.GetAsync(key);
      }

      public Task HandleUpdateAsync(Entry<K, V> baseEntry, Entry<K, V> updatedEntry) {
         return hitler.UpdateByDiffAsync(baseEntry, updatedEntry);
      }
   }
}