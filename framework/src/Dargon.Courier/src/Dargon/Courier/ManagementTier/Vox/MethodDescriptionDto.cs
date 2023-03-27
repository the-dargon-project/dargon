using System;
using System.Collections.Generic;
using Dargon.Courier.Vox;
using Dargon.Vox2;

namespace Dargon.Courier.ManagementTier {
   [VoxType((int)CourierVoxTypeIds.MethodDescriptionDto)]
   public class MethodDescriptionDto {
      public string Name { get; set; }
      public IReadOnlyList<ParameterDescriptionDto> Parameters { get; set; }
      public Type ReturnType { get; set; }
   }
}