using Dargon.Courier.Vox;
using Dargon.Vox2;
using System;

namespace Dargon.Courier.ManagementTier {
   [VoxType((int)CourierVoxTypeIds.ParameterDescriptionDto)]
   public class ParameterDescriptionDto {
      public string Name { get; set; }
      public Type Type { get; set; }
   }
}