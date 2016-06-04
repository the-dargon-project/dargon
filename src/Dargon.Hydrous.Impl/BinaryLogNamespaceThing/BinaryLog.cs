using Dargon.Commons;
using Dargon.Commons.Exceptions;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dargon.Hydrous.Impl.BinaryLogNamespaceThing {
   public class BinaryLog {
      private const string kDirectoryName = "binary_log";

      private readonly AsyncReaderWriterLock synchronization = new AsyncReaderWriterLock();
      private readonly List<BinaryLogEntry> entries = new List<BinaryLogEntry>();
      private int greatestCommittedEntryId = -1;

      public async Task UpdateGreatestCommittedEntryId(int entryId) {
         using (await synchronization.WriterLockAsync()) {
            if (greatestCommittedEntryId > entryId) {
               throw new InvalidStateException();
            } else if(entries.Count <= entryId) {
               throw new InvalidStateException();
            }
            greatestCommittedEntryId = entryId;
         }
      }

      public async Task<int> GetGreatestCommittedEntryId() {
         using (await synchronization.ReaderLockAsync()) {
            return greatestCommittedEntryId;
         }
      }

      public async Task<IReadOnlyList<BinaryLogEntry>> GetEntriesFrom(int startingEntryId) {
         using (await synchronization.ReaderLockAsync()) {
            if (entries.None()) {
               return new List<BinaryLogEntry>();
            }

            return entries.Skip(startingEntryId).ToArray();
         }
      }

      public async Task AppendAsync(object data) {
         using (await synchronization.WriterLockAsync()) {
            var entry = new BinaryLogEntry(entries.Count, data);
            entries.Add(entry);
         }
      }

      public async Task SomethingToDoWithSyncing(IReadOnlyList<BinaryLogEntry> e) {
         using (await synchronization.WriterLockAsync()) {
            if (entries.Count > 1) {
               Console.WriteLine("!");
            }
            if (entries.Count != e.First().Id) {
               throw new InvalidStateException();
            }
            entries.AddRange(e);
         }
      }
   }
}
