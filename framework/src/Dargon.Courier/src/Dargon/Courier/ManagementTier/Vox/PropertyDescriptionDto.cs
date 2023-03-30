using System;
using Dargon.Courier.Vox;
using Dargon.Vox2;

namespace Dargon.Courier.ManagementTier.Vox {
   [VoxType((int)CourierVoxTypeIds.PropertyDescriptionDto)]
   public partial class PropertyDescriptionDto {
      public string Name { get; set; }
      public Type Type { get; set; }
      public bool HasGetter { get; set; }
      public bool HasSetter { get; set; }
   }
}