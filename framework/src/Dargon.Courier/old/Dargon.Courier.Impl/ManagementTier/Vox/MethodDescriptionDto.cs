using System;
using System.Collections.Generic;
using Dargon.Vox;

namespace Dargon.Courier.ManagementTier {
   [AutoSerializable]
   public class MethodDescriptionDto {
      public string Name { get; set; }
      public IReadOnlyList<ParameterDescriptionDto> Parameters { get; set; }
      public Type ReturnType { get; set; }
   }
}