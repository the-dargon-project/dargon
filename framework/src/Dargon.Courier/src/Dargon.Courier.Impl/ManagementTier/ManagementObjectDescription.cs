using System;

namespace Dargon.Courier.ManagementTier {
   public class ManagementObjectDescription {
      public Guid Id { get; set; }
      public string Name { get; set; }
      public Type Type { get; set; }
      public object Instance { get; set; }
   }
}