using System.Threading.Tasks;
using Dargon.Courier;

namespace Dargon.Hydrous.Impl.Store.Postgre {
   public class NullCachePersistenceStrategy<K, V> : ICachePersistenceStrategy<K, V> {
      public Task<Entry<K, V>> ReadAsync(K key) => Task.FromResult(Entry<K, V>.CreateNonexistant(key));
      public Task HandleUpdateAsync(Entry<K, V> baseEntry, Entry<K, V> updatedEntry) => Task.FromResult(false);
   }
}