using System;
using Dargon.Commons.Collections;
using Dargon.Vox;

namespace Dargon.Hydrous.Impl.Vox {
   [AutoSerializable]
   public class RepartitionCompleteDto {
      public RepartitionCompleteDto() { }

      public RepartitionCompleteDto(Guid[] rankedCohortIds, System.Collections.Generic.IReadOnlyDictionary<int, IReadOnlySet<int>> partitionIdsByRank) {
         RankedCohortIds = rankedCohortIds;
         PartitionIdsByRank = partitionIdsByRank;
      }

      public System.Collections.Generic.IReadOnlyList<Guid> RankedCohortIds { get; set; }
      public System.Collections.Generic.IReadOnlyDictionary<int, IReadOnlySet<int>> PartitionIdsByRank { get; set; }
   }
}