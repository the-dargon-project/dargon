using System;
using Dargon.Courier.Vox;
using Dargon.Vox2;

namespace Dargon.Courier.ManagementTier.Vox {
   [VoxType((int)CourierVoxTypeIds.DataSetDescriptionDto)]
   public class DataSetDescriptionDto {
      public string Name { get; set; }
      public Type ElementType { get; set; }
   }
}