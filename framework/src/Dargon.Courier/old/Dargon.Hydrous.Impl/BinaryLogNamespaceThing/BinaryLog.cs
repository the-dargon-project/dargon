using Dargon.Commons;
using Dargon.Commons.AsyncPrimitives;
using Dargon.Commons.Channels;
using Dargon.Commons.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dargon.Hydrous.Impl.BinaryLogNamespaceThing {
   public class BinaryLog {
      private const string kDirectoryName = "binary_log";

      private readonly AsyncReaderWriterLock synchronization = new AsyncReaderWriterLock();
      private readonly List<BinaryLogEntry> entries = new List<BinaryLogEntry>();
      private readonly WritableChannel<BinaryLogEntry> hack__committedEntryQueue;
      private readonly Func<BinaryLogEntry, Task> hack__entrySyncAppendedCallback;
      private int greatestCommittedEntryId = -1;

      public BinaryLog(WritableChannel<BinaryLogEntry> hackCommittedEntryQueue = null, Func<BinaryLogEntry, Task> hackEntrySyncAppendedCallback = null) {
         hack__committedEntryQueue = hackCommittedEntryQueue;
         hack__entrySyncAppendedCallback = hackEntrySyncAppendedCallback;
      }

      public async Task UpdateGreatestCommittedEntryId(int entryId) {
         using (await synchronization.WriterLockAsync().ConfigureAwait(false)) {
            if (greatestCommittedEntryId > entryId) {
               throw new InvalidStateException();
            } else if(entries.Count <= entryId) {
               throw new InvalidStateException($"Attempted to advance commit pointer to {entryId} (beyond {entries.Count})");
            }
            while (greatestCommittedEntryId != entryId) {
               greatestCommittedEntryId++;
               if (hack__committedEntryQueue != null) {
                  await hack__committedEntryQueue.WriteAsync(entries[greatestCommittedEntryId]).ConfigureAwait(false);
               }
            }
         }
      }

      public async Task<int> GetGreatestCommittedEntryId() {
         using (await synchronization.ReaderLockAsync().ConfigureAwait(false)) {
            return greatestCommittedEntryId;
         }
      }


      public async Task<int> GetGreatestEntryIdAsync() {
         using (await synchronization.ReaderLockAsync().ConfigureAwait(false)) {
            return entries.Last().Id;
         }
      }

      public async Task<IReadOnlyList<BinaryLogEntry>> GetAllEntriesFrom(int startingEntryId, int endingEntryId = -1) {
         using (await synchronization.ReaderLockAsync().ConfigureAwait(false)) {
            if (entries.None()) {
               return new List<BinaryLogEntry>();
            }

            if (endingEntryId == -1) {
               return entries.Skip(startingEntryId).ToArray();
            } else {
               return entries.Skip(startingEntryId).Take(endingEntryId - startingEntryId + 1).ToArray();
            }
         }
      }

      public async Task AppendAsync(object data) {
         using (await synchronization.WriterLockAsync().ConfigureAwait(false)) {
            var entry = new BinaryLogEntry(entries.Count, data);
            entries.Add(entry);
         }
      }

      private void AppendHelper_WriterUnderLock(BinaryLogEntry entry) {
      }

      public async Task SomethingToDoWithSyncing(IReadOnlyList<BinaryLogEntry> newEntries) {
         using (await synchronization.WriterLockAsync().ConfigureAwait(false)) {
            foreach (var newEntry in newEntries) {
               var lastEntryId = entries.LastOrDefault()?.Id ?? -1;
               var nextEntryId = lastEntryId + 1;
               if (newEntry.Id == nextEntryId) {
                  // Necessary for DB read on prepare and before commit
                  // to avoid race condition where coordinator processes commit,
                  // then replica reads from DB and processes commit (double commit).
                  if (hack__entrySyncAppendedCallback != null) {
                     await hack__entrySyncAppendedCallback(newEntry).ConfigureAwait(false);
                  }

                  entries.Add(newEntry);
               } else if (newEntry.Id > nextEntryId) {
                  throw new InvalidStateException($"Expected next entry of id {nextEntryId} but got {newEntry.Id}.");
               }
            }
         }
      }
   }
}
