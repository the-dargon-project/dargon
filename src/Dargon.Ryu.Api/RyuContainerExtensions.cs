using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dargon.Ryu {
   public static class RyuContainerExtensions {
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
   }
}
