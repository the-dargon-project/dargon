using System;
using System.Collections.Generic;
using Dargon.Courier.Vox;
using Dargon.Vox2;

namespace Dargon.Courier.ManagementTier {
   [VoxType((int)CourierVoxTypeIds.MethodDescriptionDto)]
   public partial class MethodDescriptionDto {
      public string Name { get; set; }
      public List<ParameterDescriptionDto> Parameters { get; set; }
      public Type ReturnType { get; set; }
   }
}