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

   public static class RyuModuleExtensions {
      public static bool IsAutomaticLoadEnabled(this IRyuModule self) {
         return FastHasFlag(self, RyuModuleFlags.AlwaysLoad);
      }

      private static bool FastHasFlag(IRyuModule module, RyuModuleFlags flags) {
         return (module.Flags & flags) != 0;
      }

      public static RyuModule Singletons<T1, T2>(this RyuFluentOptions rfo, Func<IRyuContainer, (T1, T2)> factory) {
         (T1, T2)? resultCache = null;
         rfo.Singleton(r => (resultCache ??= factory(r)).Item1);
         rfo.Singleton(r => (resultCache ??= factory(r)).Item2);
         return rfo.Module;
      }

      public static RyuModule Singletons<T1, T2, T3>(this RyuFluentOptions rfo, Func<IRyuContainer, (T1, T2, T3)> factory) {
         (T1, T2, T3)? resultCache = null;
         rfo.Singleton(r => (resultCache ??= factory(r)).Item1);
         rfo.Singleton(r => (resultCache ??= factory(r)).Item2);
         rfo.Singleton(r => (resultCache ??= factory(r)).Item3);
         return rfo.Module;
      }

      public static RyuModule Singletons<T1, T2, T3, T4>(this RyuFluentOptions rfo, Func<IRyuContainer, (T1, T2, T3, T4)> factory) {
         (T1, T2, T3, T4)? resultCache = null;
         rfo.Singleton(r => (resultCache ??= factory(r)).Item1);
         rfo.Singleton(r => (resultCache ??= factory(r)).Item2);
         rfo.Singleton(r => (resultCache ??= factory(r)).Item3);
         rfo.Singleton(r => (resultCache ??= factory(r)).Item4);
         return rfo.Module;
      }

      public static RyuModule Singletons<T1, T2, T3, T4, T5>(this RyuFluentOptions rfo, Func<IRyuContainer, (T1, T2, T3, T4, T5)> factory) {
         (T1, T2, T3, T4, T5)? resultCache = null;
         rfo.Singleton(r => (resultCache ??= factory(r)).Item1);
         rfo.Singleton(r => (resultCache ??= factory(r)).Item2);
         rfo.Singleton(r => (resultCache ??= factory(r)).Item3);
         rfo.Singleton(r => (resultCache ??= factory(r)).Item4);
         rfo.Singleton(r => (resultCache ??= factory(r)).Item5);
         return rfo.Module;
      }

      public static RyuModule Singletons<T1, T2, T3, T4, T5, T6>(this RyuFluentOptions rfo, Func<IRyuContainer, (T1, T2, T3, T4, T5, T6)> factory) {
         (T1, T2, T3, T4, T5, T6)? resultCache = null;
         rfo.Singleton(r => (resultCache ??= factory(r)).Item1);
         rfo.Singleton(r => (resultCache ??= factory(r)).Item2);
         rfo.Singleton(r => (resultCache ??= factory(r)).Item3);
         rfo.Singleton(r => (resultCache ??= factory(r)).Item4);
         rfo.Singleton(r => (resultCache ??= factory(r)).Item5);
         rfo.Singleton(r => (resultCache ??= factory(r)).Item6);
         return rfo.Module;
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
