﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Dargon.Commons.Collections;
using Dargon.Commons.Exceptions;

namespace Dargon.Commons {
   public static class CollectionStatics {
      #region Indexing
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
      #endregion

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

      public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action) {
         foreach (var element in enumerable) {
            action(element);
         }
      }

      public static TSource MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector) {
         return source.MinBy(selector, Comparer<TKey>.Default);
      }

      /// <summary>
      /// From morelinq 
      /// http://stackoverflow.com/questions/914109/how-to-use-linq-to-select-object-with-minimum-or-maximum-property-value
      /// </summary>
      public static TSource MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector, IComparer<TKey> comparer) {
         source.ThrowIfNull("source");
         selector.ThrowIfNull("selector");
         comparer.ThrowIfNull("comparer");
         using (IEnumerator<TSource> sourceIterator = source.GetEnumerator()) {
            if (!sourceIterator.MoveNext()) {
               throw new InvalidOperationException("Sequence was empty");
            }
            TSource min = sourceIterator.Current;
            TKey minKey = selector(min);
            while (sourceIterator.MoveNext()) {
               TSource candidate = sourceIterator.Current;
               TKey candidateProjected = selector(candidate);
               if (comparer.Compare(candidateProjected, minKey) < 0) {
                  min = candidate;
                  minKey = candidateProjected;
               }
            }
            return min;
         }
      }

      public static TSource MaxBy<TSource, TKey>(this IEnumerable<TSource> source,
         Func<TSource, TKey> selector) {
         return source.MaxBy(selector, Comparer<TKey>.Default);
      }

      /// <summary>
      /// From morelinq 
      /// http://stackoverflow.com/questions/914109/how-to-use-linq-to-select-object-with-minimum-or-maximum-property-value
      /// </summary>
      public static TSource MaxBy<TSource, TKey>(this IEnumerable<TSource> source,
         Func<TSource, TKey> selector, IComparer<TKey> comparer) {
         source.ThrowIfNull("source");
         selector.ThrowIfNull("selector");
         comparer.ThrowIfNull("comparer");
         using (IEnumerator<TSource> sourceIterator = source.GetEnumerator()) {
            if (!sourceIterator.MoveNext()) {
               throw new InvalidOperationException("Sequence was empty");
            }
            TSource max = sourceIterator.Current;
            TKey maxKey = selector(max);
            while (sourceIterator.MoveNext()) {
               TSource candidate = sourceIterator.Current;
               TKey candidateProjected = selector(candidate);
               if (comparer.Compare(candidateProjected, maxKey) > 0) {
                  max = candidate;
                  maxKey = candidateProjected;
               }
            }
            return max;
         }
      }

      public static IEnumerable<Chunk<T>> Chunk<T>(this IEnumerable<T> source, int chunkSize) {
         var chunkCounter = 0;
         var current = new List<T>();
         foreach (var x in source) {
            current.Add(x);
            if (current.Count == chunkSize) {
               yield return new Chunk<T>(chunkCounter, current);
               chunkCounter++;
               current = new List<T>();
            }
         }
         if (current.Count != 0) {
            yield return new Chunk<T>(chunkCounter, current);
         }
      }

      public static IEnumerable<T> Concat<T>(this IEnumerable<T> e, T value) {
         foreach (var cur in e) {
            yield return cur;
         }
         yield return value;
      }

      public static IEnumerable<T> Concat<T>(this IEnumerable<T> e, params IEnumerable<T>[] enumerables) {
         foreach (var cur in e) {
            yield return cur;
         }
         foreach (var enumerable in enumerables)
            foreach (var value in enumerable)
               yield return value;
      }

      public static T SelectRandom<T>(this IEnumerable<T> source, Random rng = null) {
         int rand = rng?.Next() ?? StaticRandom.Next(int.MaxValue);
         var options = source.ToList();
         return options[rand % options.Count];
      }

      public static T SelectRandomWeighted<T>(this IEnumerable<T> source, Func<T, int> weighter, Random rng = null) {
         int rand = rng?.Next() ?? StaticRandom.Next(int.MaxValue);
         var options = source as List<T> ?? source.ToList();
         var optionsAndWeights = options.Map(o => new { Weight = weighter(o), Option = o });
         var totalWeight = optionsAndWeights.Sum(o => o.Weight);
         if (totalWeight == 0) {
            return options[rand % options.Count];
         }
         var weightOffset = rand % totalWeight;
         foreach (var optionAndWeight in optionsAndWeights) {
            weightOffset -= optionAndWeight.Weight;
            if (weightOffset < 0) {
               return optionAndWeight.Option;
            }
         }
         throw new InvalidStateException();
      }

      // via http://stackoverflow.com/questions/1651619/optimal-linq-query-to-get-a-random-sub-collection-shuffle
      public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source, Random rng = null) {
         if (source == null) throw new ArgumentNullException("source");

         return source.ShuffleIterator(rng ?? StaticRandom.NextRandom());
      }

      private static IEnumerable<T> ShuffleIterator<T>(this IEnumerable<T> source, Random rng) {
         var buffer = source.ToList();
         for (int i = 0; i < buffer.Count; i++) {
            int j = rng.Next(i, buffer.Count);
            yield return buffer[j];

            buffer[j] = buffer[i];
         }
      }

      // Via http://stackoverflow.com/questions/9027530/linq-not-any-vs-all-dont
      public static bool None<TSource>(this IEnumerable<TSource> source) {
         return !source.Any();
      }

      public static bool None<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate) {
         return !source.Any(predicate);
      }

      public static string Join<T>(this IEnumerable<T> e, string delimiter) {
         return String.Join(delimiter, e);
      }

      public static TAccumulate Aggregate<TSource, TAccumulate>(this IEnumerable<TSource> source, TAccumulate seed, Action<TAccumulate, TSource> action) {
         if (seed == null) {
            throw new ArgumentNullException("seed");
         }
         if (action == null) {
            throw new ArgumentNullException("action");
         }

         TAccumulate result = seed;
         foreach (TSource element in source) {
            action(result, element);
         }
         return result;
      }

      public static void AddOrThrow<K, V>(this ConcurrentDictionary<K, V> dict, K key, V value) {
         if (!dict.TryAdd(key, value)) {
            throw new InvalidStateException();
         }
      }

      public static void RemoveOrThrow<K, V>(this ConcurrentDictionary<K, V> dict, K key) {
         V existing;
         if (!dict.TryRemove(key, out existing)) {
            throw new InvalidStateException();
         }
      }

      public static void RemoveOrThrow<K, V>(this ConcurrentDictionary<K, V> dict, K key, V value) {
         V existing;
         if (!dict.TryRemove(key, out existing) || !Equals(existing, value)) {
            throw new InvalidStateException();
         }
      }

      public static IReadOnlyList<T> AsReadOnlyList<T>(this IList<T> list) => (IReadOnlyList<T>)list;

      public static IReadOnlyCollection<T> AsReadOnlyCollection<T>(this ICollection<T> list) => (IReadOnlyCollection<T>)list;

      public static IReadOnlySet<T> AsReadOnlySet<T>(this ISet<T> set) => new ReadOnlySetWrapper<T>(set);

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static T WaitThenDequeue<T>(this ConcurrentQueue<T> queue, Semaphore semaphore) {
         semaphore.WaitOne();
         return queue.DequeueOrThrow();
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static T DequeueOrThrow<T>(this ConcurrentQueue<T> queue) {
         T entry;
         if (!queue.TryDequeue(out entry)) {
            throw new InvalidStateException();
         }
         return entry;
      }
   }

   public class Chunk<T> {
      internal Chunk(int index, IReadOnlyList<T> items) {
         Index = index;
         Items = items;
      }

      public int Index { get; }
      public IReadOnlyList<T> Items { get; }
   }
}