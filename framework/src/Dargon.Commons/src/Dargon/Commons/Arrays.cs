using System;
using System.IO;

namespace Dargon.Commons {
   public static class Arrays {
      public static T[] Create<T>(int count, T elementInitializer) {
         var result = new T[count];
         result.AsSpan().Fill(elementInitializer);
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

      public static T[] Clone<T>(this T[] arr) => Clone(arr, 0, arr.Length);

      public static T[] Clone<T>(this T[] arr, int offset, int length) {
         var res = new T[length];
         Array.Copy(arr, offset, res, 0, length);
         return res;
      }

      // since Clone() is already defined
      public static T[] ShallowCloneArray<T>(this T[] arr) => Clone(arr);
      public static T[] ShallowCloneArray<T>(this T[] arr, int offset, int length) => Clone(arr, offset, length);

      public static T[] ReuseOrAllocateArrayOfCapacity<T>(ref T[] arr, int capacity) {
         if (arr == null || arr.Length < capacity) {
            arr = new T[capacity];
         }

         return arr;
      }

      public static T[] Concat<T>(T x1, T[] arr1) {
         var res = new T[1 + arr1.Length];
         res[0] = x1;
         Array.Copy(arr1, 0, res, 1, arr1.Length);
         return res;
      }

      public static T[] Concat<T>(T[] arr1, T x1) {
         var res = new T[arr1.Length + 1];
         Array.Copy(arr1, 0, res, 0, arr1.Length);
         res[arr1.Length] = x1;
         return res;
      }

      public static T[] Concat<T>(T[] arr1, T[] arr2) {
         var res = new T[arr1.Length + arr2.Length];
         Array.Copy(arr1, res, arr1.Length);
         Array.Copy(arr2, 0, res, arr1.Length, arr2.Length);
         return res;
      }

      public static T[] Concat<T>(T x1, T[] arr1, T[] arr2) {
         var res = new T[1 + arr1.Length + arr2.Length];
         res[0] = x1;
         Array.Copy(arr1, 0, res, 1, arr1.Length);
         Array.Copy(arr2, 0, res, 1 + arr1.Length, arr2.Length);
         return res;
      }

      public static T[] Concat<T>(T[] arr1, T[] arr2, T[] arr3) {
         var res = new T[arr1.Length + arr2.Length + arr3.Length];
         Array.Copy(arr1, res, arr1.Length);
         Array.Copy(arr2, 0, res, arr1.Length, arr2.Length);
         Array.Copy(arr3, 0, res, arr1.Length + arr2.Length, arr3.Length);
         return res;
      }

      public static T[] Concat<T>(T[] arr1, T[] arr2, T[] arr3, T[] arr4) {
         var res = new T[arr1.Length + arr2.Length + arr3.Length + arr4.Length];
         Array.Copy(arr1, res, arr1.Length);
         Array.Copy(arr2, 0, res, arr1.Length, arr2.Length);
         Array.Copy(arr3, 0, res, arr1.Length + arr2.Length, arr3.Length);
         Array.Copy(arr4, 0, res, arr1.Length + arr2.Length + arr3.Length, arr4.Length);
         return res;
      }

      public static bool IsNullOrEmpty<T>(T[] arr) => arr == null || arr.Length == 0;
   }
}
