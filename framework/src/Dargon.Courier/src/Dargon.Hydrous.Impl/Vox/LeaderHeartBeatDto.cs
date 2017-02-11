using System;
using Dargon.Commons.Collections;
using Dargon.Vox;

namespace Dargon.Hydrous.Impl.Vox {
   [AutoSerializable]
   public class LeaderHeartBeatDto {
      public IReadOnlySet<Guid> CohortIds { get; set; }
   }
}