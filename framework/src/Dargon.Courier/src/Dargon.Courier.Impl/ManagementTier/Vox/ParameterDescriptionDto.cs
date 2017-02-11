using System;
using Dargon.Vox;

namespace Dargon.Courier.ManagementTier {
   [AutoSerializable]
   public class ParameterDescriptionDto {
      public string Name { get; set; }
      public Type Type { get; set; }
   }
}