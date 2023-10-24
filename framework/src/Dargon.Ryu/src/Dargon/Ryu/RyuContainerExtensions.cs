using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dargon.Ryu.Modules;

namespace Dargon.Ryu {
   public static class RyuContainerExtensions {
      public static T GetOrActivate<T>(this IRyuContainer container) {
         return (T)container.GetOrActivate(typeof(T));
      }

      public static T Create<T>(this IRyuContainer container) {
         return (T)container.ActivateUntracked(typeof(T));
      }

      public static T GetOrDefault<T>(this IRyuContainer container) {
         T result;
         container.TryGet<T>(out result);
         return result;
      }

      public static T GetOrThrow<T>(this IRyuContainer container) {
         return (T)container.GetOrThrow(typeof(T));
      }

      public static object GetOrThrow(this IRyuContainer container, Type type) {
         object result;
         if (!container.TryGet(type, out result)) {
            throw new RyuGetException(type, null);
         }
         return result;
      }

      public static bool TryGet<T>(this IRyuContainer container, out T value) {
         object output;
         var result = container.TryGet(typeof(T), out output);
         value = (T)output;
         return result;
      }

      public static IEnumerable<T> Find<T>(this IRyuContainer container) {
         return container.Find(typeof(T)).Cast<T>();
      }

      public static void Set<T>(this IRyuContainer container, T instance) {
         container.Set(typeof(T), instance);
      }

      public static void SetMultiple<T1, T2>(this IRyuContainer container, T1 i1, T2 i2) {
         container.Set(typeof(T1), i1);
         container.Set(typeof(T2), i2);
      }

      public static void SetMultiple<T1, T2, T3>(this IRyuContainer container, T1 i1, T2 i2, T3 i3) {
         container.SetMultiple(i1, i2);
         container.Set(typeof(T3), i3);
      }

      public static void SetMultiple<T1, T2, T3, T4>(this IRyuContainer container, T1 i1, T2 i2, T3 i3, T4 i4) {
         container.SetMultiple(i1, i2, i3);
         container.Set(typeof(T4), i4);
      }

      public static void SetMultiple<T1, T2, T3, T4, T5>(this IRyuContainer container, T1 i1, T2 i2, T3 i3, T4 i4, T5 i5) {
         container.SetMultiple(i1, i2, i3);
         container.SetMultiple(i4, i5);
      }

      public static void SetMultiple<T1, T2, T3, T4, T5, T6>(this IRyuContainer container, T1 i1, T2 i2, T3 i3, T4 i4, T5 i5, T6 i6) {
         container.SetMultiple(i1, i2, i3);
         container.SetMultiple(i4, i5, i6);
      }

      public static void SetMultiple<T1, T2, T3, T4, T5, T6, T7>(this IRyuContainer container, T1 i1, T2 i2, T3 i3, T4 i4, T5 i5, T6 i6, T7 i7) {
         container.SetMultiple(i1, i2, i3);
         container.SetMultiple(i4, i5, i6, i7);
      }

      public static void SetMultiple<T1, T2, T3, T4, T5, T6, T7, T8>(this IRyuContainer container, T1 i1, T2 i2, T3 i3, T4 i4, T5 i5, T6 i6, T7 i7, T8 i8) {
         container.SetMultiple(i1, i2, i3, i4);
         container.SetMultiple(i5, i6, i7, i8);
      }

      public static void SetMultiple<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this IRyuContainer container, T1 i1, T2 i2, T3 i3, T4 i4, T5 i5, T6 i6, T7 i7, T8 i8, T9 i9) {
         container.SetMultiple(i1, i2, i3, i4);
         container.SetMultiple(i5, i6, i7, i8, i9);
      }

      public static void SetMultiple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this IRyuContainer container, T1 i1, T2 i2, T3 i3, T4 i4, T5 i5, T6 i6, T7 i7, T8 i8, T9 i9, T10 i10) {
         container.SetMultiple(i1, i2, i3, i4, i5);
         container.SetMultiple(i6, i7, i8, i9, i10);
      }

      public static IRyuContainer Create(this RyuFactory factory, RyuConfiguration configuration, IRyuModule[] modules) {
         var c = factory.Create(configuration);
         c.ImportModules(modules);
         return c;
      }

      public static IRyuContainer Create(this RyuFactory factory, IRyuModule[] modules) {
         var c = factory.Create();
         c.ImportModules(modules);
         return c;
      }

      public static IRyuContainer CreateChildContainer(this IRyuContainer container, IRyuModule[] modules) {
         var c = container.CreateChildContainer();
         c.ImportModules(modules);
         return c;
      }

      public static void ImportModules(this IRyuContainer container, IRyuModule[] modules) {
         var moduleImporter = container.GetOrThrow<IRyuFacade>().ModuleImporter;
         moduleImporter.ImportModules((RyuContainer)container, modules);
      }
   }
}
