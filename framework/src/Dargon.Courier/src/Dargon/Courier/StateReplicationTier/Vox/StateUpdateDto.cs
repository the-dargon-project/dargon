﻿using Dargon.Courier.Vox;
using Dargon.Vox2;

namespace Dargon.Courier.StateReplicationTier.Vox {
   [VoxType((int)CourierVoxTypeIds.StateTierBaseId)]
   public partial class StateUpdateDto {
      public const int kSnapshotDeltaSeq = 0;

      public bool IsSnapshot;
      public bool IsOutOfBand;
      public int SnapshotEpoch;
      public int DeltaSeq;
      [P] public object Payload;
      public int VanityTotalSeq;
   }
}