using System;
using System.IO;

namespace Dargon.Commons {
   public static class Arrays {
      public static T[] Create<T>(int count, T elementInitializer) {
         var result = new T[count];
         for (var i = 0; i < result.Length; i++) {
            result[i] = elementInitializer;
         }
         return result;
      }

      public static T[] Create<T>(int count, Func<T> elementInitializer) {
         var result = new T[count];
         for (var i = 0; i < result.Length; i++) {
            result[i] = elementInitializer();
         }
         return result;
      }

      public static T[] Create<T>(int count, Func<int, T> elementInitializer) {
         var result = new T[count];
         for (var i = 0; i < result.Length; i++) {
            result[i] = elementInitializer(i);
         }
         return result;
      }
   }
}
