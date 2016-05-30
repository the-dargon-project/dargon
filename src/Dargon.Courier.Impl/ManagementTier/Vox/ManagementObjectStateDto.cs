using System.Collections.Generic;
using Dargon.Vox;

namespace Dargon.Courier.ManagementTier {
   [AutoSerializable]
   public class ManagementObjectStateDto {
      public IReadOnlyList<MethodDescriptionDto> Methods { get; set; }
   }
}