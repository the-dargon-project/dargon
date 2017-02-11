using System;
using Dargon.Vox;

namespace Dargon.Courier.ManagementTier.Vox {
   [AutoSerializable]
   public class DataSetDescriptionDto {
      public string Name { get; set; }
      public Type ElementType { get; set; }
   }
}