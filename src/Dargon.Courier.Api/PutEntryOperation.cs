using System;
using System.Threading.Tasks;
using Dargon.Vox;
using NLog;

namespace Dargon.Courier {
   [AutoSerializable]
   public class PutEntryOperation<K, V> : IEntryOperation<K, V, Entry<K, V>> {
      private static readonly Logger logger = LogManager.GetCurrentClassLogger();

      public PutEntryOperation() { }

      public Guid Id { get; private set; }
      public V Value { get; private set; }
      public EntryOperationType Type => EntryOperationType.Put;
      public Task<Entry<K, V>> ExecuteAsync(Entry<K, V> entry) {
         logger.Debug($"Exec Put {{ V = {Value} }} on {entry}");

         var oldEntry = entry.DeepCloneSerializable();
         logger.Debug($"Exec Put cloned to {oldEntry}.");

         entry.Value = Value;

         logger.Debug($"Exec Put returning {oldEntry}.");
         return Task.FromResult(oldEntry);
      }

      public static PutEntryOperation<K, V> Create(V value) => new PutEntryOperation<K, V> {
         Id = Guid.NewGuid(),
         Value = value
      };
   }
}