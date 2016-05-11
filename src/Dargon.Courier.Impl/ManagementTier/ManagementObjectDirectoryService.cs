using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Dargon.Vox;

namespace Dargon.Courier.ManagementTier {
   [Guid("776C1CEC-44DE-40CA-9ED4-0F942BC3A8DC")]
   public interface IManagementObjectDirectoryService {
      IEnumerable<Guid> EnumerateManagementObjectIds();
      ManagementObjectDescriptionDto GetManagementObjectDescription(Guid managementObjectId);
   }

   public class ManagementObjectDirectoryService : IManagementObjectDirectoryService {
      private readonly ManagementObjectRegistry managementObjectRegistry;

      public ManagementObjectDirectoryService(ManagementObjectRegistry managementObjectRegistry) {
         this.managementObjectRegistry = managementObjectRegistry;
      }

      public IEnumerable<Guid> EnumerateManagementObjectIds() {
         return managementObjectRegistry.EnumerateManagementObjectIds();
      }

      public ManagementObjectDescriptionDto GetManagementObjectDescription(Guid managementObjectId) {
         return managementObjectRegistry.GetManagementObjectDescription(managementObjectId);
      }
   }

   [AutoSerializable]
   public class ManagementObjectDescriptionDto {
      public IReadOnlyList<MethodDescriptionDto> Methods { get; set; }
   }

   [AutoSerializable]
   public class MethodDescriptionDto {
      public string Name { get; set; }
      public IReadOnlyList<ParameterDescriptionDto> Parameters { get; set; }
      public Type ReturnType { get; set; }
   }

   [AutoSerializable]
   public class ParameterDescriptionDto {
      public string Name { get; set; }
      public Type Type { get; set; }
   }
}
