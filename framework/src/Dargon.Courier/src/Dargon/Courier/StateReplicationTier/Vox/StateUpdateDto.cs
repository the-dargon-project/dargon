namespace Dargon.Courier.StateReplicationTier.Vox {
   public class StateUpdateDto {
      public const int kSnapshotDeltaSeq = 0;

      public bool IsSnapshot;
      public int SnapshotEpoch;
      public int DeltaSeq;
      public object Payload;
      public int VanityTotalSeq;
   }
}