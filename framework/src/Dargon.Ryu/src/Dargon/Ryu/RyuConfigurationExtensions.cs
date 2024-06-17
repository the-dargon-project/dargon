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

      public static RyuFluentAdditions<TupleBox<(T1, T2)>> Singletons<T1, T2>(this RyuFluentOptions rfo, Func<IRyuContainerForUserActivator, (T1, T2)> factory) {
         var ret = rfo.Singleton<TupleBox<(T1, T2)>>(r => new(factory(r)));
         rfo.Singleton(async r => (await r.GetOrActivateAsync<TupleBox<(T1, T2)>>()).Value.Item1);
         rfo.Singleton(async r => (await r.GetOrActivateAsync<TupleBox<(T1, T2)>>()).Value.Item2);
         return ret;
      }

      public static RyuFluentAdditions<TupleBox<(T1, T2, T3)>> Singletons<T1, T2, T3>(this RyuFluentOptions rfo, Func<IRyuContainerForUserActivator, (T1, T2, T3)> factory) {
         var ret = rfo.Singleton<TupleBox<(T1, T2, T3)>>(r => new(factory(r)));
         rfo.Singleton(async r => (await r.GetOrActivateAsync<TupleBox<(T1, T2, T3)>>()).Value.Item1);
         rfo.Singleton(async r => (await r.GetOrActivateAsync<TupleBox<(T1, T2, T3)>>()).Value.Item2);
         rfo.Singleton(async r => (await r.GetOrActivateAsync<TupleBox<(T1, T2, T3)>>()).Value.Item3);
         return ret;
      }

      public static RyuFluentAdditions<TupleBox<(T1, T2, T3, T4)>> Singletons<T1, T2, T3, T4>(this RyuFluentOptions rfo, Func<IRyuContainerForUserActivator, (T1, T2, T3, T4)> factory) {
         var ret = rfo.Singleton<TupleBox<(T1, T2, T3, T4)>>(r => new(factory(r)));
         rfo.Singleton(async r => (await r.GetOrActivateAsync<TupleBox<(T1, T2, T3, T4)>>()).Value.Item1);
         rfo.Singleton(async r => (await r.GetOrActivateAsync<TupleBox<(T1, T2, T3, T4)>>()).Value.Item2);
         rfo.Singleton(async r => (await r.GetOrActivateAsync<TupleBox<(T1, T2, T3, T4)>>()).Value.Item3);
         rfo.Singleton(async r => (await r.GetOrActivateAsync<TupleBox<(T1, T2, T3, T4)>>()).Value.Item4);
         return ret;
      }

      public static RyuFluentAdditions<TupleBox<(T1, T2, T3, T4, T5)>> Singletons<T1, T2, T3, T4, T5>(this RyuFluentOptions rfo, Func<IRyuContainerForUserActivator, (T1, T2, T3, T4, T5)> factory) {
         var ret = rfo.Singleton<TupleBox<(T1, T2, T3, T4, T5)>>(r => new(factory(r)));
         rfo.Singleton(async r => (await r.GetOrActivateAsync<TupleBox<(T1, T2, T3, T4, T5)>>()).Value.Item1);
         rfo.Singleton(async r => (await r.GetOrActivateAsync<TupleBox<(T1, T2, T3, T4, T5)>>()).Value.Item2);
         rfo.Singleton(async r => (await r.GetOrActivateAsync<TupleBox<(T1, T2, T3, T4, T5)>>()).Value.Item3);
         rfo.Singleton(async r => (await r.GetOrActivateAsync<TupleBox<(T1, T2, T3, T4, T5)>>()).Value.Item4);
         rfo.Singleton(async r => (await r.GetOrActivateAsync<TupleBox<(T1, T2, T3, T4, T5)>>()).Value.Item5);
         return ret;
      }

      public static RyuFluentAdditions<TupleBox<(T1, T2, T3, T4, T5, T6)>> Singletons<T1, T2, T3, T4, T5, T6>(this RyuFluentOptions rfo, Func<IRyuContainerForUserActivator, (T1, T2, T3, T4, T5, T6)> factory) {
         var ret = rfo.Singleton<TupleBox<(T1, T2, T3, T4, T5, T6)>>(r => new(factory(r)));
         rfo.Singleton(async r => (await r.GetOrActivateAsync<TupleBox<(T1, T2, T3, T4, T5, T6)>>()).Value.Item1);
         rfo.Singleton(async r => (await r.GetOrActivateAsync<TupleBox<(T1, T2, T3, T4, T5, T6)>>()).Value.Item2);
         rfo.Singleton(async r => (await r.GetOrActivateAsync<TupleBox<(T1, T2, T3, T4, T5, T6)>>()).Value.Item3);
         rfo.Singleton(async r => (await r.GetOrActivateAsync<TupleBox<(T1, T2, T3, T4, T5, T6)>>()).Value.Item4);
         rfo.Singleton(async r => (await r.GetOrActivateAsync<TupleBox<(T1, T2, T3, T4, T5, T6)>>()).Value.Item5);
         rfo.Singleton(async r => (await r.GetOrActivateAsync<TupleBox<(T1, T2, T3, T4, T5, T6)>>()).Value.Item6);
         return ret;
      }

      public static RyuFluentAdditions<TupleBox<(T1, T2)>> Singletons<T1, T2>(this RyuFluentOptions rfo, Func<IRyuContainerForUserActivator, Task<(T1, T2)>> factory) {
         var ret = rfo.Singleton<TupleBox<(T1, T2)>>(async r => new(await factory(r)));
         rfo.Singleton(async r => (await r.GetOrActivateAsync<TupleBox<(T1, T2)>>()).Value.Item1);
         rfo.Singleton(async r => (await r.GetOrActivateAsync<TupleBox<(T1, T2)>>()).Value.Item2);
         return ret;
      }

      public static RyuFluentAdditions<TupleBox<(T1, T2, T3)>> Singletons<T1, T2, T3>(this RyuFluentOptions rfo, Func<IRyuContainerForUserActivator, Task<(T1, T2, T3)>> factory) {
         var ret = rfo.Singleton<TupleBox<(T1, T2, T3)>>(async r => new(await factory(r)));
         rfo.Singleton(async r => (await r.GetOrActivateAsync<TupleBox<(T1, T2, T3)>>()).Value.Item1);
         rfo.Singleton(async r => (await r.GetOrActivateAsync<TupleBox<(T1, T2, T3)>>()).Value.Item2);
         rfo.Singleton(async r => (await r.GetOrActivateAsync<TupleBox<(T1, T2, T3)>>()).Value.Item3);
         return ret;
      }

      public static RyuFluentAdditions<TupleBox<(T1, T2, T3, T4)>> Singletons<T1, T2, T3, T4>(this RyuFluentOptions rfo, Func<IRyuContainerForUserActivator, Task<(T1, T2, T3, T4)>> factory) {
         var ret = rfo.Singleton<TupleBox<(T1, T2, T3, T4)>>(async r => new(await factory(r)));
         rfo.Singleton(async r => (await r.GetOrActivateAsync<TupleBox<(T1, T2, T3, T4)>>()).Value.Item1);
         rfo.Singleton(async r => (await r.GetOrActivateAsync<TupleBox<(T1, T2, T3, T4)>>()).Value.Item2);
         rfo.Singleton(async r => (await r.GetOrActivateAsync<TupleBox<(T1, T2, T3, T4)>>()).Value.Item3);
         rfo.Singleton(async r => (await r.GetOrActivateAsync<TupleBox<(T1, T2, T3, T4)>>()).Value.Item4);
         return ret;
      }

      public static RyuFluentAdditions<TupleBox<(T1, T2, T3, T4, T5)>> Singletons<T1, T2, T3, T4, T5>(this RyuFluentOptions rfo, Func<IRyuContainerForUserActivator, Task<(T1, T2, T3, T4, T5)>> factory) {
         var ret = rfo.Singleton<TupleBox<(T1, T2, T3, T4, T5)>>(async r => new(await factory(r)));
         rfo.Singleton(async r => (await r.GetOrActivateAsync<TupleBox<(T1, T2, T3, T4, T5)>>()).Value.Item1);
         rfo.Singleton(async r => (await r.GetOrActivateAsync<TupleBox<(T1, T2, T3, T4, T5)>>()).Value.Item2);
         rfo.Singleton(async r => (await r.GetOrActivateAsync<TupleBox<(T1, T2, T3, T4, T5)>>()).Value.Item3);
         rfo.Singleton(async r => (await r.GetOrActivateAsync<TupleBox<(T1, T2, T3, T4, T5)>>()).Value.Item4);
         rfo.Singleton(async r => (await r.GetOrActivateAsync<TupleBox<(T1, T2, T3, T4, T5)>>()).Value.Item5);
         return ret;
      }

      public static RyuFluentAdditions<TupleBox<(T1, T2, T3, T4, T5, T6)>> Singletons<T1, T2, T3, T4, T5, T6>(this RyuFluentOptions rfo, Func<IRyuContainerForUserActivator, Task<(T1, T2, T3, T4, T5, T6)>> factory) {
         var ret = rfo.Singleton<TupleBox<(T1, T2, T3, T4, T5, T6)>>(async r => new(await factory(r)));
         rfo.Singleton(async r => (await r.GetOrActivateAsync<TupleBox<(T1, T2, T3, T4, T5, T6)>>()).Value.Item1);
         rfo.Singleton(async r => (await r.GetOrActivateAsync<TupleBox<(T1, T2, T3, T4, T5, T6)>>()).Value.Item2);
         rfo.Singleton(async r => (await r.GetOrActivateAsync<TupleBox<(T1, T2, T3, T4, T5, T6)>>()).Value.Item3);
         rfo.Singleton(async r => (await r.GetOrActivateAsync<TupleBox<(T1, T2, T3, T4, T5, T6)>>()).Value.Item4);
         rfo.Singleton(async r => (await r.GetOrActivateAsync<TupleBox<(T1, T2, T3, T4, T5, T6)>>()).Value.Item5);
         rfo.Singleton(async r => (await r.GetOrActivateAsync<TupleBox<(T1, T2, T3, T4, T5, T6)>>()).Value.Item6);
         return ret;
      }

      public record TupleBox<T>(T Value) where T : struct;
   }

   public static class RyuTypeExtensions {
      public static bool IsRequired(this RyuType type) {
         return (type.Flags & RyuTypeFlags.Required) != 0;
      }

      public static bool IsEventual(this RyuType type) {
         return (type.Flags & RyuTypeFlags.Eventual) != 0;
      }

      public static bool IsSingleton(this RyuType type) {
         return (type.Flags & RyuTypeFlags.Singleton) != 0;
      }

      public static bool NeedsMainThread(this RyuType type) {
         return (type.Flags & RyuTypeFlags.RequiresMainThread) != 0;
      }
   }
}
