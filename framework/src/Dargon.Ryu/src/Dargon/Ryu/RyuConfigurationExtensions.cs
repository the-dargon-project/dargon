using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dargon.Ryu.Modules;

namespace Dargon.Ryu {
   public static class RyuConfigurationExtensions {
//      public static bool IsDirectoryAssemblyLoadingEnabled(this RyuConfiguration configuration) {
//         return (configuration.LoadingStrategy & LoadingStrategyFlags.DisableDirectoryAssemblyLoading) == 0;
//      }
   }

   internal static class RyuModuleExtensions {
      public static bool IsAutomaticLoadEnabled(this IRyuModule self) {
         return FastHasFlag(self, RyuModuleFlags.AlwaysLoad);
      }

      private static bool FastHasFlag(IRyuModule module, RyuModuleFlags flags) {
         return (module.Flags & flags) != 0;
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
