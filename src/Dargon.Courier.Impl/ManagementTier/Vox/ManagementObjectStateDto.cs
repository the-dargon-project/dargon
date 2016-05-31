using System.Collections.Generic;
using Dargon.Courier.ManagementTier.Vox;
using Dargon.Vox;

namespace Dargon.Courier.ManagementTier {
   [AutoSerializable]
   public class ManagementObjectStateDto {
      public IReadOnlyList<MethodDescriptionDto> Methods { get; set; }
      public IReadOnlyList<PropertyDescriptionDto> Properties { get; set; }
   }
}