using System.Collections.Generic;
using Dargon.Courier.ManagementTier.Vox;
using Dargon.Courier.Vox;
using Dargon.Vox2;

namespace Dargon.Courier.ManagementTier {
   [VoxType((int)CourierVoxTypeIds.ManagementObjectStateDto)]
   public class ManagementObjectStateDto {
      public IReadOnlyList<MethodDescriptionDto> Methods { get; set; }
      public IReadOnlyList<PropertyDescriptionDto> Properties { get; set; }
      public IReadOnlyList<DataSetDescriptionDto> DataSets { get; set; }
   }
}