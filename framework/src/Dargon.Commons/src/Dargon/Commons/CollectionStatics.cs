using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Dargon.Commons.Collections;
using Dargon.Commons.Exceptions;

namespace Dargon.Commons {
   public static class CollectionStatics {
      public static ExposedArrayList<T> AddIfNotNull<T>(this ExposedArrayList<T> eal, T x) where T : class {
         if (x != null) {
            eal.Add(x);
         }

         return eal;
      }

      public static void Add<T>(this List<T> l, (bool cond, T item) x) {
         if (x.cond) {
            l.Add(x.item);
         }
      }

      public static void Add<T>(this ExposedArrayList<T> eal, (bool cond, T item) x) {
         if (x.cond) {
            eal.Add(x.item);
         }
      }

      public static void AddRange<T>(this ICollection<T> c, IEnumerable<T> items) {
         foreach (T x in items) {
            c.Add(x);
         }
      }

      public static bool TryFindFirst<T>(this IEnumerable<T> enumerable, Predicate<T> predicate, out T firstMatch) {
         foreach (var x in enumerable) {
            if (predicate(x)) {
               firstMatch = x;
               return true;
            }
         }
         firstMatch = default(T);
         return false;
      }

      public static bool TryFindFirstIndex<T>(this IList<T> list, Predicate<T> predicate, out int firstMatchIndex) {
         return TryFindFirstIndex(list, predicate, 0, list.Count, out firstMatchIndex);
      }

      public static bool TryFindFirstIndex<T>(this IList<T> list, Predicate<T> predicate, int startIndex, int length, out int firstMatchIndex) {
         for(int i = 0, j = startIndex; i < length; i++, j++) {
            if (predicate(list[j])) {
               firstMatchIndex = j;
               return true;
            }
         }

         firstMatchIndex = -1;
         return false;
      }

      public static T FirstAndOnly<T>(this T[] arr) {
         if (arr.Length == 0) throw new ArgumentOutOfRangeException("No element 0");
         else if (arr.Length >= 2) throw new InvalidOperationException("More than one element!");
         return arr[0];
      }

      public static T FirstAndOnly<T>(this IEnumerable<T> e) {
         if (e is T[] arr) {
            return FirstAndOnly(arr);
         }

         using (var it = e.GetEnumerator()) {
            if (!it.MoveNext()) throw new ArgumentOutOfRangeException("No element 0");
            var res = it.Current;
            if (it.MoveNext()) throw new InvalidOperationException("More than one element!");
            return res;
         }
      }

      public static T FirstAndOnlyOrDefault<T>(this IEnumerable<T> e) {
         using (var it = e.GetEnumerator()) {
            if (!it.MoveNext()) return default;
            var res = it.Current;
            if (it.MoveNext()) return default;
            return res;
         }
      }

      public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> source, out TKey Key, out TValue Value) {
         Key = source.Key;
         Value = source.Value;
      }

      public static void Deconstruct<TKey, TValue>(this IGrouping<TKey, TValue> source, out TKey Key, out IEnumerable<TValue> Value) {
         Key = source.Key;
         Value = source;
      }

      #region Indexing
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static T Get<T>(this T[] collection, int index) {
         return collection[index];
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static T Get<T>(this IList<T> collection, int index) {
         return collection[index];
      }

      public static V Get<K, V>(this Dictionary<K, V> dict, K key) => dict[key];

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

      /* // Now implemented in System.Collections.Generic.CollectionExtensions
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static V GetValueOrDefault<K, V>(this IReadOnlyDictionary<K, V> dict, K key) {
         V result;
         dict.TryGetValue(key, out result);
         return result;
      }
      */

      public static bool TryGetValueNullable<K, V>(this Dictionary<K, V>? dict, K key, out V value) {
         if (dict == null) {
            value = default;
            return false;
         }

         return dict.TryGetValue(key, out value);
      }

      public static T[] Index<T>(this T[] arr, int[] indices) {
         var res = new T[indices.Length];
         for (var i = 0; i < indices.Length; i++) {
            res[i] = arr[indices[i]];
         }
         return res;
      }

      public static T[] LogicalIndex<T>(this IReadOnlyList<T> input, IReadOnlyList<bool> indexConditions, bool negateConditions = false) {
         if (input.Count != indexConditions.Count)
            throw new ArgumentException("Size mismatch between inputs.");

         var passCount = indexConditions.Count(x => x);
         var result = new T[negateConditions ? indexConditions.Count - passCount : passCount];
         int resultIndex = 0;
         for (var i = 0; i < indexConditions.Count && resultIndex < result.Length; i++) {
            if (indexConditions[i] ^ negateConditions) {
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

      public static bool Add<K, V>(this Dictionary<K, HashSet<V>> dict, K key, V value) {
         HashSet<V> set;
         if (!dict.TryGetValue(key, out set)) {
            set = new HashSet<V>();
            dict[key] = set;
         }
         return set.Add(value);
      }

      public static bool Remove<K, V>(this Dictionary<K, HashSet<V>> dict, K key, V value) {
         HashSet<V> set;
         if (!dict.TryGetValue(key, out set)) {
            return false;
         }
         var res = set.Remove(value);
         if (set.Count == 0) {
            dict.Remove(key);
         }
         return res;
      }

      public struct TEnumerateWithIndexEnumerator<T, TEnumerator> : IEnumerator<(int index, T value)> where TEnumerator : IEnumerator<T> {
         private TEnumerator inner;
         private int index;

         public TEnumerateWithIndexEnumerator(TEnumerator inner) {
            this.inner = inner;
            this.index = -1;
         }

         public bool MoveNext() {
            if (inner.MoveNext()) {
               index++;
               return true;
            }
            return false;
         }

         public void Reset() {
            inner.Reset();
            index = -1;
         }

         public (int index, T value) Current => (index, inner.Current);
         object IEnumerator.Current => Current;

         public void Dispose() {
            inner.Dispose();
         }
      }

      public static EnumeratorToEnumerableAdapter<(int, T), TEnumerateWithIndexEnumerator<T, ArrayEnumerator<T>>> Enumerate<T>(this T[] items) =>
         EnumeratorToEnumerableAdapter<(int, T)>.Create(
            new TEnumerateWithIndexEnumerator<T, ArrayEnumerator<T>>(
               new ArrayEnumerator<T>(items)));

      public static EnumeratorToEnumerableAdapter<(int, T), TEnumerateWithIndexEnumerator<T, List<T>.Enumerator>> Enumerate<T>(this List<T> items) =>
         EnumeratorToEnumerableAdapter<(int, T)>.Create(
            new TEnumerateWithIndexEnumerator<T, List<T>.Enumerator>(
               items.GetEnumerator()));

      public static EnumeratorToEnumerableAdapter<(int, T), TEnumerateWithIndexEnumerator<T, HashSet<T>.Enumerator>> Enumerate<T>(this HashSet<T> items) =>
         EnumeratorToEnumerableAdapter<(int, T)>.Create(
            new TEnumerateWithIndexEnumerator<T, HashSet<T>.Enumerator>(
               items.GetEnumerator()));

      public static EnumeratorToEnumerableAdapter<(int, KeyValuePair<K, V>), TEnumerateWithIndexEnumerator<KeyValuePair<K, V>, Dictionary<K, V>.Enumerator>> Enumerate<K, V>(this Dictionary<K, V> items) =>
         EnumeratorToEnumerableAdapter<(int, KeyValuePair<K, V>)>.Create(
            new TEnumerateWithIndexEnumerator<KeyValuePair<K, V>, Dictionary<K, V>.Enumerator>(
               items.GetEnumerator()));

      public static EnumeratorToEnumerableAdapter<(int, KeyValuePair<K, V>), TEnumerateWithIndexEnumerator<KeyValuePair<K, V>, SortedDictionary<K, V>.Enumerator>> Enumerate<K, V>(this SortedDictionary<K, V> items) =>
         EnumeratorToEnumerableAdapter<(int, KeyValuePair<K, V>)>.Create(
            new TEnumerateWithIndexEnumerator<KeyValuePair<K, V>, SortedDictionary<K, V>.Enumerator>(
               items.GetEnumerator()));

      public static EnumeratorToEnumerableAdapter<(int, T), TEnumerateWithIndexEnumerator<T, ExposedArrayList<T>.Enumerator>> Enumerate<T>(this ExposedArrayList<T> items) =>
         EnumeratorToEnumerableAdapter<(int, T)>.Create(
            new TEnumerateWithIndexEnumerator<T, ExposedArrayList<T>.Enumerator>(
               items.GetEnumerator()));

      public static EnumeratorToEnumerableAdapter<(int, ExposedKeyValuePair<K, V>), TEnumerateWithIndexEnumerator<ExposedKeyValuePair<K, V>, ExposedArrayList<ExposedKeyValuePair<K, V>>.Enumerator>> Enumerate<K, V>(this ExposedListDictionary<K, V> items)
         => Enumerate(items.list);

      public static T[] ToReversedArray<T>(this T[] items) {
         var res = new T[items.Length];
         var ni = res.Length - 1;
         for (var i = 0; i < items.Length; i++) {
            res[ni] = items[i];
            ni--;
         }
         return res;
      }

      public static T[] ToReversedArray<T>(this List<T> items) {
         var res = new T[items.Count];
         var ni = res.Length - 1;
         for (var i = 0; i < items.Count; i++) {
            res[ni] = items[i];
            ni--;
         }
         return res;
      }

      public static T[] ToReversedArray<T>(this ExposedArrayList<T> items) {
         var res = new T[items.Count];
         var ni = res.Length - 1;
         for (var i = 0; i < items.Count; i++) {
            res[ni] = items[i];
            ni--;
         }
         return res;
      }

      public static U[] Map<T, U>(this IReadOnlyList<T> arr, Func<T, U> projector) {
         U[] result = new U[arr.Count];
         for (var i = 0; i < result.Length; i++) {
            result[i] = projector(arr[i]);
         }
         return result;
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static U[] Map<T, U>(this IReadOnlyList<T> arr, Func<T, int, U> projector) {
         var result = new U[arr.Count];
         for (int i = 0; i < arr.Count; i++) {
            result[i] = projector(arr[i], i);
         }
         return result;
      }

      public static U[] Map<T, U>(this IReadOnlyList<T> arr, int offset, int length, Func<T, U> projector) {
         U[] result = new U[length];
         for (var i = 0; i < result.Length; i++) {
            result[i] = projector(arr[offset + i]);
         }
         return result;
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static U[] Map<T, U>(this IReadOnlyList<T> arr, int offset, int length, Func<T, int, U> projector) {
         var result = new U[length];
         for (int i = 0; i < arr.Count; i++) {
            result[i] = projector(arr[offset + i], offset + i);
         }
         return result;
      }

      public static List<U> MapList<T, U>(this IReadOnlyList<T> arr, Func<T, U> projector) {
         var result = new List<U>(arr.Count);
         for (var i = 0; i < arr.Count; i++) {
            result.Add(projector(arr[i]));
         }
         return result;
      }

      public static List<U> MapList<T, U>(this IReadOnlyList<T> arr, Func<U> projector) {
         var result = new List<U>(arr.Count);
         for (var i = 0; i < arr.Count; i++) {
            result.Add(projector());
         }
         return result;
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static List<U> MapList<T, U>(this IReadOnlyList<T> arr, Func<T, int, U> projector) {
         var result = new List<U>(arr.Count);
         for (int i = 0; i < arr.Count; i++) {
            result.Add(projector(arr[i], i));
         }
         return result;
      }


      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static Dictionary<K, R> Map<K, V, R>(this IReadOnlyDictionary<K, V> dict, Func<K, V, R> map) {
         if (dict is Dictionary<K, V> d) {
            return d.Map(map);
         }
         return dict.ToDictionary(kvp => kvp.Key, kvp => map(kvp.Key, kvp.Value));
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static Dictionary<RK, RV> Map<K, V, RK, RV>(this IReadOnlyDictionary<K, V> dict, Func<K, V, RK> kmap, Func<K, V, RV> vmap) {
         return dict.ToDictionary(kvp => kmap(kvp.Key, kvp.Value), kvp => vmap(kvp.Key, kvp.Value));
      }

      public static U[] MapMany<T, U>(this T[] arr, Func<T, IReadOnlyList<U>> cheapMap) {
         var result = new U[arr.Sum(x => cheapMap(x).Count)];
         var nextIndex = 0;
         for (var i = 0; i < arr.Length; i++) {
            var x = cheapMap(arr[i]);
            for (var j = 0; j < x.Count; j++) {
               result[nextIndex] = x[j];
               nextIndex++;
            }
         }
         return result;
      }

      public static Dictionary<K, V> ToDictionary<K, V>(this Dictionary<K, V> dict) => dict.Map(v => v);

      public static IEnumerable<Tuple<T, U>> Zip<T, U>(this IEnumerable<T> e1, IEnumerable<U> e2) => e1.Zip(e2, Tuple.Create);

      public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action) {
         if (enumerable.GetType().IsArray) {
            foreach (var element in (T[])(object)enumerable) {
               action(element);
            }
         } else {
            foreach (var element in enumerable) {
               action(element);
            }
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

      //http://stackoverflow.com/questions/4681949/use-linq-to-group-a-sequence-of-numbers-with-no-gaps
      public static IEnumerable<IEnumerable<T>> GroupAdjacentBy<T>(
         this IEnumerable<T> source, Func<T, T, bool> predicate) {
         using (var e = source.GetEnumerator()) {
            if (e.MoveNext()) {
               var list = new List<T> { e.Current };
               var pred = e.Current;
               while (e.MoveNext()) {
                  if (predicate(pred, e.Current)) {
                     list.Add(e.Current);
                  } else {
                     yield return list;
                     list = new List<T> { e.Current };
                  }
                  pred = e.Current;
               }
               yield return list;
            }
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
         int rand = rng?.Next() ?? StaticRandom.Next(Int32.MaxValue);
         var options = source.ToList();
         return options[rand % options.Count];
      }

      public static T SelectRandomWeighted<T>(this IEnumerable<T> source, Func<T, int> weighter, Random rng = null) {
         int rand = rng?.Next() ?? StaticRandom.Next(Int32.MaxValue);
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

      public static IEnumerable<T> RotateLeft<T>(this IEnumerable<T> e) {
         var it = e.GetEnumerator();
         if (!it.MoveNext()) {
            yield break;
         }
         var first = it.Current;
         while (it.MoveNext()) {
            yield return it.Current;
         }
         yield return first;
      }

      public static IEnumerable<T> RotateLeft<T>(this IEnumerable<T> e, int n) {
         var front = new List<T>();

         using (var it = e.GetEnumerator()) {
            for (var i = 0; i < n; i++) {
               if (it.MoveNext()) {
                  front.Add(it.Current);
               } else {
                  throw new NotImplementedException();
               }
            }

            while (it.MoveNext()) {
               yield return it.Current;
            }

            foreach (var x in front) {
               yield return x;
            }
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

      public static IReadOnlyList<T> CastToReadOnlyList<T>(this IList<T> list) {
         return (IReadOnlyList<T>)list;
      }

#if !NETCOREAPP
      public static HashSet<T> ToHashSet<T>(this IEnumerable<T> e) => new HashSet<T>(e);
#endif

      public static MultiValueDictionary<K, V> ToMultiValueDictionary<I, K, V>(this IReadOnlyList<I> coll, Func<I, K> keyMapper, Func<I, V> valueMapper) {
         var dict = new MultiValueDictionary<K, V>();
         for (var i = 0; i < coll.Count; i++) {
            var x = coll[i];
            dict.Add(keyMapper(x), valueMapper(x));
         }
         return dict;
      }

      public static T[] ToArray<T>(this IEnumerable<T> e, int len) {
         var enumerator = e.GetEnumerator();
         var result = new T[len];
         for (var i = 0; i < len; i++) {
            if (!enumerator.MoveNext()) {
               throw new IndexOutOfRangeException($"Enumerator didn't yield enough items. Stopped at i={i} of len={len}.");
            }
            result[i] = enumerator.Current;
         }
         return result;
      }

      public static void Resize<T>(this List<T> list, int size) {
         if (size < list.Count) {
            list.RemoveRange(size, list.Count - size);
         } else if (size > list.Count) {
            list.AddRange(new T[size - list.Count]);
         }
      }

      public static IEnumerable<T> MergeSorted<T, U>(this IEnumerable<T> first, IEnumerable<T> second, Func<T, U> cheapKeyFunc) {
         return MergeSorted(
            first,
            second,
            (a, b) => Comparer<U>.Default.Compare(cheapKeyFunc(a), cheapKeyFunc(b)));
      }

      // See https://stackoverflow.com/questions/9807701/is-there-an-easy-way-to-merge-two-ordered-sequences-using-linq
      public static IEnumerable<T> MergeSorted<T>(this IEnumerable<T> first, IEnumerable<T> second, Func<T, T, int> comparer) {
         using (var firstEnumerator = first.GetEnumerator())
         using (var secondEnumerator = second.GetEnumerator()) {

            var elementsLeftInFirst = firstEnumerator.MoveNext();
            var elementsLeftInSecond = secondEnumerator.MoveNext();
            while (elementsLeftInFirst || elementsLeftInSecond) {
               if (!elementsLeftInFirst) {
                  do {
                     yield return secondEnumerator.Current;
                  } while (secondEnumerator.MoveNext());
                  yield break;
               }

               if (!elementsLeftInSecond) {
                  do {
                     yield return firstEnumerator.Current;
                  } while (firstEnumerator.MoveNext());
                  yield break;
               }

               if (comparer(firstEnumerator.Current, secondEnumerator.Current) < 0) {
                  yield return firstEnumerator.Current;
                  elementsLeftInFirst = firstEnumerator.MoveNext();
               } else {
                  yield return secondEnumerator.Current;
                  elementsLeftInSecond = secondEnumerator.MoveNext();
               }
            }
         }
      }

      public static Range RangeWithLength(this int startInclusive, int length) => RangeToExclusive(startInclusive, startInclusive + length);
      public static Range RangeToInclusive(this int startInclusive, int endInclusive) => new Range(startInclusive, endInclusive + 1);
      public static Range RangeToExclusive(this int startInclusive, int endExclusive) => new Range(startInclusive, endExclusive);

      public static void ApplySubtract<T>(this HashSet<T> self, HashSet<T> other) {
         foreach (var x in other) self.Remove(x);
      }

      public static HashSet<T> ComputeSubtract<T>(this HashSet<T> self, HashSet<T> other) {
         var res = new HashSet<T>(self);
         
         foreach (var x in other) {
            res.Remove(x);
         }

         return res;
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