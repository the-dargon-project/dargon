using System.Collections.Generic;
using System.Threading.Tasks;
using Dargon.Commons.Collections;

namespace Dargon.Courier {
   public interface ICache<K, V> {
      Task<IReadableEntry<K, V>> GetAsync(K key);
      Task<IReadableEntry<K, V>> PutAsync(K key, V value);
      Task<R> ProcessAsync<R>(K key, IEntryOperation<K, V, R> operation);
      Task<IReadOnlyDictionary<K, R>> ProcessManyAsync<R>(IReadOnlySet<K> keys, IEntryOperation<K, V, R> operation);
   }

   public interface IReadableEntry<K, V> {
      K Key { get; }
      V Value { get; }
      bool Exists { get; }
   }

   public interface IEntry<K, V> : IReadableEntry<K, V> {
      new V Value { get; set; }
      bool IsDirty { get; set; }
   }
}
