using System;
using System.Threading.Tasks;

namespace Dargon.Courier {
   public interface IEntryOperation {
      Guid Id { get; }
      EntryOperationType Type { get; }
   }

   public interface IEntryOperation<K, V, TResult> : IEntryOperation {
      Task<TResult> ExecuteAsync(Entry<K, V> entry);
   }
}