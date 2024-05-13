using Dargon.Courier.StateReplicationTier.Primaries;
using Dargon.Courier.Vox;
using Dargon.Vox2;

namespace Dargon.Courier.StateReplicationTier.Vox {
   [VoxType((int)CourierVoxTypeIds.StateUpdateDto)]
   public partial class StateUpdateDto {
      public const int kSnapshotDeltaSeq = 0;

      public bool IsSnapshot;
      public bool IsOutOfBand;
      public ReplicationVersion Version;
      [P] public object Payload;
      public int VanityTotalSeq;
   }
}