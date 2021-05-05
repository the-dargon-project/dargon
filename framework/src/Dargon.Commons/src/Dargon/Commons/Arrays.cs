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

      /// <summary>
      /// Fisher-Yates shuffle
      /// </summary>
      public static T[] ShuffleInPlace<T>(this T[] arr, Random rng) {
         for (var i = arr.Length - 1; i > 0; i--) {
            var j = rng.Next(i + 1);
            (arr[j], arr[i]) = (arr[i], arr[j]);
         }

         return arr;
      }
   }
}
