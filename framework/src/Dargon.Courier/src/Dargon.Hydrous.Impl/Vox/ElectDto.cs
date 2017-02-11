using System;
using Dargon.Commons.Collections;
using Dargon.Vox;

namespace Dargon.Hydrous.Impl.Vox {
   [AutoSerializable]
   public class ElectDto {
      public Guid NomineeId { get; set; }
      public IReadOnlySet<Guid> FollowerIds { get; set; }
   }
}