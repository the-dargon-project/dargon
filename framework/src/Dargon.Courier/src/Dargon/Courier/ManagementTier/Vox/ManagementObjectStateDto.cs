using System.Collections.Generic;
using Dargon.Courier.ManagementTier.Vox;
using Dargon.Courier.Vox;
using Dargon.Vox2;

namespace Dargon.Courier.ManagementTier {
   [VoxType((int)CourierVoxTypeIds.ManagementObjectStateDto)]
   public partial class ManagementObjectStateDto {
      public List<MethodDescriptionDto> Methods { get; set; }
      public List<PropertyDescriptionDto> Properties { get; set; }
      public List<DataSetDescriptionDto> DataSets { get; set; }
   }
}