using System;
using System.Collections.Generic;
using Dargon.Vox;

namespace Dargon.Courier.ManagementTier.Vox {
   [AutoSerializable]
   public class PropertyDescriptionDto {
      public string Name { get; set; }
      public Type Type { get; set; }
      public bool HasGetter { get; set; }
      public bool HasSetter { get; set; }
   }
}