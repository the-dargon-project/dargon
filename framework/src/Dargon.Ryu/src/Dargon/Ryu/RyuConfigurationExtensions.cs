using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dargon.Ryu.Modules;

namespace Dargon.Ryu {
   public static class RyuConfigurationExtensions {
      public static bool IsDirectoryAssemblyLoadingEnabled(this RyuConfiguration configuration) {
         return (configuration.LoadingStrategy & LoadingStrategyFlags.DisableDirectoryAssemblyLoading) == 0;
      }
   }

   public static class ModuleConfigurationExtensions {
      public static bool IsManualLoadRequired(this ModuleConfigurationAttribute self) {
         return self != null && (self.Options & ModuleOptionFlags.ManualLoad) != 0;
      }
   }

   public static class RyuTypeExtensions {
      public static bool IsRequired(this RyuType type) {
         return (type.Flags & RyuTypeFlags.Required) != 0;
      }

      public static bool IsSingleton(this RyuType type) {
         return (type.Flags & RyuTypeFlags.Cache) != 0;
      }
   }
}
