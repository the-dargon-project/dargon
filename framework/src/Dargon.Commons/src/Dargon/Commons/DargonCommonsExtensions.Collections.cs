﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Threading;
using Dargon.Commons.Collections;
using Dargon.Commons.Exceptions;

namespace Dargon.Commons {
   public static partial class DargonCommonsExtensions {
      public static int Sum(this int[] arr) {
         int result = 0;
         for (int i = 0; i < arr.Length; i++) {
            result += arr[i];
         }
         return result;
      }

      public static int Sum<T>(this T[] arr, Func<T, int> map) {
         int result = 0;
         for (int i = 0; i < arr.Length; i++) {
            result += map(arr[i]);
         }
         return result;
      }

      public static U[] Map<T, U>(this IReadOnlyList<T> arr, Func<T, U> projector) {
         U[] result = new U[arr.Count];
         for (var i = 0; i < result.Length; i++) {
            result[i] = projector(arr[i]);
         }
         return result;
      }

      public static U[] Map<T, U>(this IReadOnlyList<T> arr, Func<U> projector) {
         U[] result = new U[arr.Count];
         for (var i = 0; i < result.Length; i++) {
            result[i] = projector();
         }
         return result;
      }

      public static void ForEach<T>(this T[] arr, Action<T> action) {
         for (int i = 0; i < arr.Length; i++) {
            action(arr[i]);
         }
      }

      public static T[] LogicalIndex<T>(this IReadOnlyList<T> input, IReadOnlyList<bool> indexConditions) {
         if (input.Count != indexConditions.Count)
            throw new ArgumentException("Size mismatch between inputs.");

         var result = new T[indexConditions.Count(x => x)];
         int resultIndex = 0;
         for (var i = 0; i < indexConditions.Count && resultIndex < result.Length; i++) {
            if (indexConditions[i]) {
               result[resultIndex] = input[i];
               resultIndex++;
            }
         }
         return result;
      }

      public static string LogicalIndex(this string s, bool[] indexConditions) {
         return new string(s.ToCharArray().LogicalIndex(indexConditions));
      }

      public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action) {
         foreach (var element in enumerable) {
            action(element);
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

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static T Get<T>(this T[] collection, int index) {
         return collection[index];
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static T Get<T>(this IList<T> collection, int index) {
         return collection[index];
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static V Get<K, V>(this IDictionary<K, V> dict, K key) {
         return dict[key];
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static V GetValueOrDefault<K, V>(this Dictionary<K, V> dict, K key) {
         return ((IDictionary<K, V>)dict).GetValueOrDefault(key);
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static V GetValueOrDefault<K, V>(this IDictionary<K, V> dict, K key) {
         V result;
         dict.TryGetValue(key, out result);
         return result;
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static V GetValueOrDefault<K, V>(this IReadOnlyDictionary<K, V> dict, K key) {
         V result;
         dict.TryGetValue(key, out result);
         return result;
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static T WaitThenDequeue<T>(this IConcurrentQueue<T> queue, Semaphore semaphore) {
         semaphore.WaitOne();
         return queue.DequeueOrThrow();
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static T DequeueOrThrow<T>(this IConcurrentQueue<T> queue) {
         T entry;
         if (!queue.TryDequeue(out entry)) {
            throw new InvalidStateException();
         }
         return entry;
      }
   }
}
