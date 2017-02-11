using System;
using Dargon.Vox;

namespace Dargon.Courier.ManagementTier {
   [AutoSerializable]
   public class ManagementObjectIdentifierDto {
      public Guid Id { get; set; }
      public string FullName { get; set; }
   }
}