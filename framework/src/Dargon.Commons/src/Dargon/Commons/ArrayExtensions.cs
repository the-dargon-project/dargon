using System;

namespace Dargon.Commons {
   public static class ArrayExtensions {
      public static void ForEach<T>(this T[] arr, Action<T> action) {
         for (int i = 0; i < arr.Length; i++) {
            action(arr[i]);
         }
      }

      public static T[] SubArray<T>(this T[] data, int index) {
         return SubArray(data, index, data.Length - index);
      }

      public static T[] SubArray<T>(this T[] data, int index, int length) {
         T[] result = new T[length];
         Array.Copy(data, index, result, 0, length);
         return result;
      }
   }
}