using Dargon.Commons.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Dargon.Commons.CollectionStatics;

namespace Dargon.Commons {
   public static class CollectionStatics_NetCore {
      public static ExposedArrayList<T> AddIfNotNull<T>(this ExposedArrayList<T> eal, T x) where T : class {
         if (x != null) {
            eal.Add(x);
         }

         return eal;
      }

      public static void Add<T>(this ExposedArrayList<T> eal, (bool cond, T item) x) {
         if (x.cond) {
            eal.Add(x.item);
         }
      }

      public static void Deconstruct(this Range range, out int offset, out int length) {
         range.Start.IsFromEnd.AssertIsFalse();
         range.End.IsFromEnd.AssertIsFalse();

         offset = range.Start.Value;
         length = range.End.Value - range.Start.Value;
      }

      public static EnumeratorToEnumerableAdapter<(int Index, T Item), TEnumerateWithIndexEnumerator<T, ExposedArrayList<T>.Enumerator>> Enumerate<T>(this ExposedArrayList<T> items) =>
         EnumeratorToEnumerableAdapter<(int, T)>.Create(
            new TEnumerateWithIndexEnumerator<T, ExposedArrayList<T>.Enumerator>(
               items.GetEnumerator()));

      public static EnumeratorToEnumerableAdapter<(int Index, ExposedKeyValuePair<K, V> Item), TEnumerateWithIndexEnumerator<ExposedKeyValuePair<K, V>, ExposedArrayList<ExposedKeyValuePair<K, V>>.Enumerator>> Enumerate<K, V>(this ExposedListDictionary<K, V> items)
         => Enumerate(items.list);

      public static T[] ToReversedArray<T>(this ExposedArrayList<T> items) {
         var res = new T[items.Count];
         var ni = res.Length - 1;
         for (var i = 0; i < items.Count; i++) {
            res[ni] = items[i];
            ni--;
         }

         return res;
      }

      public static Dictionary<K, V> ToDictionary<K, V>(this Dictionary<K, V> dict) => dict.Map(v => v);

      public static IReadOnlySet<T> AsReadOnlySet<T>(this ISet<T> set) => new ReadOnlySetWrapper<T>(set);

      public static Range RangeWithLength(this int startInclusive, int length) => RangeToExclusive(startInclusive, startInclusive + length);
      public static Range RangeToInclusive(this int startInclusive, int endInclusive) => new Range(startInclusive, endInclusive + 1);
      public static Range RangeToExclusive(this int startInclusive, int endExclusive) => new Range(startInclusive, endExclusive);

      public class EnumerateAdjacentGroupingRangesEnumerator<T, K> : IEnumerable<Range>, IEnumerator<Range> where K : IEquatable<K> {
         private readonly T[] arr;
         private readonly int startIndexInclusive;
         private readonly int endIndexExclusive;
         private readonly Func<T, K> itemToGroupKeyFunc;
         private int currentIndex;
         private Range current;

         public EnumerateAdjacentGroupingRangesEnumerator(T[] arr, int startIndexInclusive, int endIndexExclusive, Func<T, K> itemToGroupKeyFunc) {
            this.arr = arr;
            this.startIndexInclusive = startIndexInclusive;
            this.endIndexExclusive = endIndexExclusive;
            this.itemToGroupKeyFunc = itemToGroupKeyFunc;
            this.currentIndex = startIndexInclusive - 1;
            this.current = default;
         }

         public bool MoveNext() {
            // Precondition: CurrentIndex points to the inclusive end of the prior group
            // Postcondition: CurrentIndex points to the inclusive end of the current group
            if (currentIndex + 1 == endIndexExclusive) {
               current = new(int.MinValue, int.MinValue + 1);
               return false;
            }
            currentIndex++; // move from inclusive end of prior group to inclusive start of current group
            var groupStartIndexInclusive = currentIndex;
            var key = itemToGroupKeyFunc(arr[currentIndex]);
            while (currentIndex + 1 != endIndexExclusive && key.Equals(itemToGroupKeyFunc(arr[currentIndex + 1]))) {
               currentIndex++;
            }
            var groupEndIndexInclusive = currentIndex;
            current = new(groupStartIndexInclusive, groupEndIndexInclusive + 1);
            return true;
         }

         public void Reset() {
            currentIndex = startIndexInclusive - 1;
            this.current = default;
         }

         public Range Current => current;
         object IEnumerator.Current => Current;

         public void Dispose() { }

         public IEnumerator<Range> GetEnumerator() => this;
         IEnumerator IEnumerable.GetEnumerator() => this;
      }

      public static EnumerateAdjacentGroupingRangesEnumerator<T, K> EnumerateAdjacentGroupingIndexRanges<T, K>(this T[] arr, Func<T, K> itemToGroupKeyFunc) where K : IEquatable<K>
         => new(arr, 0, arr.Length, itemToGroupKeyFunc);

      public static EnumerateAdjacentGroupingRangesEnumerator<T, K> EnumerateAdjacentGroupingIndexRanges<T, K>(this T[] arr, int offset, int length, Func<T, K> itemToGroupKeyFunc) where K : IEquatable<K>
         => new(arr, offset, offset + length, itemToGroupKeyFunc);

      public static EnumerateAdjacentGroupingRangesEnumerator<T, K> EnumerateAdjacentGroupingIndexRanges<T, K>(this ExposedArrayList<T> eal, Func<T, K> itemToGroupKeyFunc) where K : IEquatable<K>
         => new(eal.store, 0, eal.size, itemToGroupKeyFunc);

      public static EnumerateAdjacentGroupingRangesEnumerator<T, K> EnumerateAdjacentGroupingIndexRanges<T, K>(this ExposedArrayList<T> eal, int offset, int length, Func<T, K> itemToGroupKeyFunc) where K : IEquatable<K>
         => new(eal.store, offset.AssertIsLessThanOrEqualTo(eal.size), (offset + length).AssertIsLessThanOrEqualTo(eal.size), itemToGroupKeyFunc);

   }
}
