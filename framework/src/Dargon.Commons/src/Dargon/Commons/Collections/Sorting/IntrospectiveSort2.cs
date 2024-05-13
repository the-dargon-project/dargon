using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using Dargon.Commons.Templating;

namespace Dargon.Commons.Collections.Sorting;

/// <summary>
/// Based on MIT-Licensed .NET Core System.Array.SorterObjectArray
/// https://github.com/dotnet/runtime/blob/f58fde50376479ad3ba1339d37df816f90bff287/src/libraries/System.Private.CoreLib/src/System/Array.cs
/// </summary>
public static class IntrospectiveSort2 {
   /// <summary>
   /// Sorts an array of <paramref name="indices"/> referring to the contents of <paramref name="items"/>, which itself
   /// is not mutated.
   /// </summary>
   public static void IndirectSort<TItem, TComparer>(this int[] indices, TItem[] items, TComparer comparer) where TComparer : struct, IFastComparer<TItem> {
      IndirectSort(indices, 0, indices.Length, items, comparer);
   }

   /// <inheritdoc cref="IndirectSort{TItem,TComparer}(int[],TItem[],TComparer)"/>
   public static void IndirectSort<TItem, TComparer>(this int[] indices, ExposedArrayList<TItem> items, TComparer comparer) where TComparer : struct, IFastComparer<TItem> {
      items.size.AssertIsGreaterThanOrEqualTo(indices.Length);
      IndirectSort(indices, 0, indices.Length, items.store, comparer);
   }

   /// <summary>
   /// Sorts an array of <paramref name="indices"/> referring to the contents of <paramref name="items"/>, which itself
   /// is not mutated. The offset and length determine the block within <paramref name="indices"/> to sort.
   /// </summary>
   public static void IndirectSort<TItem, TComparer>(this int[] indices, int offset, int length, TItem[] items, TComparer comparer) where TComparer : struct, IFastComparer<TItem> {
      var indexingComparer = new IndexingComparerProxy<TItem, TComparer>(items, comparer);
      var sorter = new Sorter<int, object, IndexingComparerProxy<TItem, TComparer>, TFalse>(indices, null, ref indexingComparer);
      sorter.Sort(offset, length);
   }

   /// <summary>
   /// Sorts an array of <paramref name="indices"/> referring to the contents of <paramref name="items"/>, which itself
   /// is not mutated.
   /// </summary>
   public static void Sort<TItem, TComparer>(this TItem[] items, TComparer comparer) where TComparer : struct, IFastComparer<TItem> {
      Sort(items, 0, items.Length, comparer);
   }

   /// <inheritdoc cref="IndirectSort{TItem,TComparer}(int[],TItem[],TComparer)"/>
   public static void Sort<TItem, TComparer>(this ExposedArrayList<TItem> items, TComparer comparer) where TComparer : struct, IFastComparer<TItem> {
      Sort(items.store, 0, items.Count, comparer);
   }

   /// <summary>
   /// Sorts an array of <paramref name="indices"/> referring to the contents of <paramref name="items"/>, which itself
   /// is not mutated. The offset and length determine the block within <paramref name="indices"/> to sort.
   /// </summary>
   public static void Sort<TItem, TComparer>(this TItem[] items, int offset, int length, TComparer comparer) where TComparer : struct, IFastComparer<TItem> {
      var sorter = new Sorter<TItem, object, TComparer, TFalse>(items, null, ref comparer);
      sorter.Sort(offset, length);
   }

   private struct IndexingComparerProxy<TItem, TComparer> : IFastComparer<int> where TComparer : struct, IFastComparer<TItem> {
      private readonly TItem[] items;
      private TComparer inner;

      public IndexingComparerProxy(TItem[] items, TComparer inner) {
         this.items = items;
         this.inner = inner;
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public bool GreaterThan(int x, int y) => inner.GreaterThan(items[x], items[y]);
      
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public bool LessThan(int x, int y) => inner.LessThan(items[x], items[y]);
      
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public bool Equals(int x, int y) => inner.Equals(items[x], items[y]);
   }

   // Private value used by the Sort methods for instances of Array.
   // This is slower than the one for Object[], since we can't use the JIT helpers
   // to access the elements.  We must use GetValue & SetValue.
   public ref struct Sorter<TKey, TItem, TKeyComparer, TBoolSupportsItems> where TKeyComparer : struct, IFastComparer<TKey> {
      // This is the threshold where Introspective sort switches to Insertion sort.
      // Empirically, 16 seems to speed up most cases without slowing down others, at least for integers.
      // Large value types may benefit from a smaller number.
      internal const int IntrosortSizeThreshold = 16;

      private readonly TKey[] keys;
      private readonly TItem[] items;
      public ref TKeyComparer Comparer;

      public Sorter(TKey[] keys, TItem[] items, ref TKeyComparer comparer) {
         this.keys = keys;
         this.items = items;
         this.Comparer = ref comparer;
      }

      private void SwapIfGreater(int i, int j) {
         if (i != j) {
            if (keys[i].IsGreaterThan(keys[j], Comparer)) {
               Swap(i, j);
            }
         }
      }

      private void Swap(int i, int j) {
         (keys[i], keys[j]) = (keys[j], keys[i]);

         if (TBool.IsTrue<TBoolSupportsItems>()) {
            (items[i], items[j]) = (items[j], items[i]);
         }
      }

      public void Sort(int left, int length) {
         IntrospectiveSort(left, length);
      }

      private void IntrospectiveSort(int left, int length) {
         if (length < 2)
            return;

         try {
            IntroSort(left, length + left - 1, 2 * (BitOperations.Log2((uint)length) + 1));
         } catch (IndexOutOfRangeException e) {
            throw new ArgumentException("Bad Comparer", e);
         } catch (Exception e) {
            throw new InvalidOperationException("Comparer Failed", e);
         }
      }

      private void IntroSort(int lo, int hi, int depthLimit) {
         Debug.Assert(hi >= lo);
         Debug.Assert(depthLimit >= 0);

         while (hi > lo) {
            int partitionSize = hi - lo + 1;
            if (partitionSize <= IntrosortSizeThreshold) {
               Debug.Assert(partitionSize >= 2);

               if (partitionSize == 2) {
                  SwapIfGreater(lo, hi);
                  return;
               }

               if (partitionSize == 3) {
                  SwapIfGreater(lo, hi - 1);
                  SwapIfGreater(lo, hi);
                  SwapIfGreater(hi - 1, hi);
                  return;
               }

               InsertionSort(lo, hi);
               return;
            }

            if (depthLimit == 0) {
               Heapsort(lo, hi);
               return;
            }
            depthLimit--;

            int p = PickPivotAndPartition(lo, hi);
            IntroSort(p + 1, hi, depthLimit);
            hi = p - 1;
         }
      }

      private int PickPivotAndPartition(int lo, int hi) {
         Debug.Assert(hi - lo >= IntrosortSizeThreshold);

         // Compute median-of-three.  But also partition them, since we've done the comparison.
         int mid = lo + (hi - lo) / 2;

         SwapIfGreater(lo, mid);
         SwapIfGreater(lo, hi);
         SwapIfGreater(mid, hi);

         TKey pivot = keys[mid];
         Swap(mid, hi - 1);
         int left = lo, right = hi - 1;  // We already partitioned lo and hi and put the pivot in hi - 1.  And we pre-increment & decrement below.

         while (left < right) {
            while (keys[++left].IsLessThan(pivot, Comparer)) ;
            while (pivot.IsLessThan(keys[--right], Comparer)) ;

            if (left >= right)
               break;

            Swap(left, right);
         }

         // Put pivot in the right location.
         if (left != hi - 1) {
            Swap(left, hi - 1);
         }
         return left;
      }

      private void Heapsort(int lo, int hi) {
         int n = hi - lo + 1;
         for (int i = n / 2; i >= 1; i--) {
            DownHeap(i, n, lo);
         }
         for (int i = n; i > 1; i--) {
            Swap(lo, lo + i - 1);

            DownHeap(1, i - 1, lo);
         }
      }

      private void DownHeap(int i, int n, int lo) {
         TKey d = keys[lo + i - 1];
         TItem dt = default;
         if (TBool.IsTrue<TBoolSupportsItems>()) {
            dt = items[lo + i - 1];
         }

         int child;
         while (i <= n / 2) {
            child = 2 * i;
            if (child < n && keys[lo + child - 1].IsLessThan(keys[lo + child], Comparer)) {
               child++;
            }

            if (!d.IsLessThan(keys[lo + child - 1], Comparer))
               break;

            keys[lo + i - 1] = keys[lo + child - 1];
            if (TBool.IsTrue<TBoolSupportsItems>())
               items[lo + i - 1] = items[lo + child - 1];

            i = child;
         }
         keys[lo + i - 1] = d;
         if (TBool.IsTrue<TBoolSupportsItems>())
            items[lo + i - 1] = dt;
      }

      private void InsertionSort(int lo, int hi) {
         int i, j;
         TKey t;
         TItem dt = default;
         for (i = lo; i < hi; i++) {
            j = i;
            t = keys[i + 1];
            if (TBool.IsTrue<TBoolSupportsItems>()) {
               dt = items[i + 1];
            }

            while (j >= lo && t.IsLessThan(keys[j], Comparer)) {
               keys[j + 1] = keys[j];
               if (TBool.IsTrue<TBoolSupportsItems>())
                  items[j + 1] = items[j];
               j--;
            }

            keys[j + 1] = t;
            if (TBool.IsTrue<TBoolSupportsItems>())
               items[j + 1] = dt;
         }
      }
   }
}