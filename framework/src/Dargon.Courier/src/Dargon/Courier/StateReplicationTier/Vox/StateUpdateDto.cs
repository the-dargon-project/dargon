using Dargon.Vox;

namespace Dargon.Courier.StateReplicationTier.Vox {
   [AutoSerializable]
   public class StateUpdateDto {
      public const int kSnapshotDeltaSeq = 0;

      public bool IsSnapshot;
      public bool IsOutOfBand;
      public int SnapshotEpoch;
      public int DeltaSeq;
      public object Payload;
      public int VanityTotalSeq;
   }
}