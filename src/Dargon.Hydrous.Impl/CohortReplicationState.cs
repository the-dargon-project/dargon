using System.Threading.Tasks;
using Dargon.Commons.Exceptions;
using Dargon.Vox;
using Nito.AsyncEx;

namespace Dargon.Hydrous.Impl {
   [AutoSerializable]
   public class CohortReplicationState {
      private readonly AsyncLock asyncLock = new AsyncLock();

      public int NextEntryIdToSync { get; private set; }
      public int GreatestCommittedEntryId { get; private set; } = -1;

      public async Task UpdateNextEntryIdToSync(int nextEntryIdToSync) {
         using (await asyncLock.LockAsync()) {
            if (NextEntryIdToSync > nextEntryIdToSync) {
               throw new InvalidStateException();
            }
            NextEntryIdToSync = nextEntryIdToSync;
         }
      }

      public async Task UpdateGreatestCommittedEntryId(int newGreatestCommittedEntryId) {
         using (await asyncLock.LockAsync()) {
            if (GreatestCommittedEntryId > newGreatestCommittedEntryId) {
               throw new InvalidStateException();
            }
            GreatestCommittedEntryId = newGreatestCommittedEntryId;
         }
      }
   }
}