using Dargon.Vox;
using System;
using System.Threading.Tasks;

namespace Dargon.Hydrous {
   [AutoSerializable]
   public class ReadEntryOperation<K, V> : IEntryOperation<K, V, Entry<K, V>> {
      public ReadEntryOperation() { }

      public Guid Id { get; private set; }
      public EntryOperationType Type => EntryOperationType.Read;

      public Task<Entry<K, V>> ExecuteAsync(Entry<K, V> entry) {
         return Task.FromResult(entry);
      }

      public static ReadEntryOperation<K, V> Create() => new ReadEntryOperation<K, V> {
         Id = Guid.NewGuid()
      };
   }
}