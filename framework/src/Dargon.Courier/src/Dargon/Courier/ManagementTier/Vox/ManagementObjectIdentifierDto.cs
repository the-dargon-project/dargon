using System;
using Dargon.Courier.Vox;
using Dargon.Vox2;

namespace Dargon.Courier.ManagementTier {
   [VoxType((int)CourierVoxTypeIds.ManagementObjectIdentifierDto)]
   public class ManagementObjectIdentifierDto {
      public Guid Id { get; set; }
      public string FullName { get; set; }
   }
}