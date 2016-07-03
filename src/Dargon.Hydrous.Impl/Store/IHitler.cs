using System.Threading.Tasks;
using Dargon.Courier;

namespace Dargon.Hydrous.Impl.Store {
   public interface IHitler<K, V> {
      Task<int> ClearAsync();
      Task<Entry<K, V>> InsertAsync(V item);
      Task<Entry<K, V>> GetAsync(K key);
      Task UpdateByDiffAsync(Entry<K, V> existing, Entry<K, V> updated);
      Task PutAsync(K key, V value);
   }
}