using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dargon.Commons.Collections {
   /// <summary>
   /// (Ripped from PlayOn)
   /// 
   /// Because no other implementation exists that doesn't suck.
   /// Traditional minheap-based pq. Allows duplicate entry, supports resizing.
   /// </summary>
   public class PriorityQueue<TItem> : IReadOnlyCollection<TItem> {
      // 1 + 4 + 16 + 64 = 5 + 16 + 64 = 21 + 64 = 85
      private const int HeapBranchFactor = 4;
      private const int InitialCapacity = 1 + HeapBranchFactor;
      private const bool DisableValidation = true;

      private readonly ComparerProxy<TItem> comparer;
      private TItem[] storage = new TItem[InitialCapacity];
      private int size;

      public PriorityQueue() : this(Comparer<TItem>.Default) { }

      public PriorityQueue(IComparer<TItem> itemComparer)
         : this(new ComparerProxy<TItem> { comparer = itemComparer }) { }

      public PriorityQueue(Comparison<TItem> itemComparisonFunc)
         : this(new ComparerProxy<TItem> { comparison = itemComparisonFunc }) { }

      private PriorityQueue(ComparerProxy<TItem> comparerProxy) {
         comparer = comparerProxy;
      }

      public int Capacity => storage.Length;
      public bool IsEmpty => size == 0;
      public int Count => size;

      public bool Any() => size != 0;

      public TItem Peek() {
         return size == 0 
            ? throw new InvalidOperationException("The queue is empty") 
            : storage[0];
      }

      public bool TryPeek(out TItem item) {
         item = size > 0 ? storage[0] : default;
         return size > 0;
      }

      public void Enqueue(TItem item) {
         // expand heap 1 level if storage is too small to fit another item.
         if (size == storage.Length) {
            var newStorage = new TItem[storage.Length * HeapBranchFactor + 1];
            Array.Copy(storage, newStorage, storage.Length);
            storage = newStorage;
         }

         // imaginary put of item into index _size
         size++;

         // start percolating inserted item up from its current index, _size
         var childIndex = size - 1;
         while (childIndex > 0) {
            // if inserted item is greater than or equal to its parent, stop percolation.
            var parentIndex = (childIndex - 1) / HeapBranchFactor;
            if (comparer.Compare(item, storage[parentIndex]) >= 0)
               break;

            // Otherwise shift its parent down and continue percolating one level closer to root.
            storage[childIndex] = storage[parentIndex];
            childIndex = parentIndex;
         }

         // actually place item into the heap
         storage[childIndex] = item;
      }

      public bool TryDequeue(out TItem item) {
         if (size == 0) {
            item = default(TItem);
            return false;
         }

         // remove heap head
         item = storage[0];
         size--;

         // remove heap tail
         var tail = storage[size];
         storage[size] = default(TItem);

         // percolate heap tail down from root; at iteration start, _storage[parentIndex] needs to be filled.
         var parentIndex = 0;
         while (true) {
            ComputeChildrenIndices(parentIndex, out var childrenStartIndexInclusive, out var childrenEndIndexExclusive);
            var childCount = childrenEndIndexExclusive - childrenStartIndexInclusive;

            // cannot percolate down beyond leaf node
            if (childCount == 0) {
               storage[parentIndex] = tail;
               break;
            }

            // find smallest child
            var smallestChildIndex = childrenStartIndexInclusive;
            for (var i = childrenStartIndexInclusive + 1; i < childrenEndIndexExclusive; i++) {
               if (comparer.Compare(storage[smallestChildIndex], storage[i]) > 0) {
                  smallestChildIndex = i;
               }
            }

            // if smallest child is greater than the percolate down subject (heap tail), end percolation
            if (comparer.Compare(storage[smallestChildIndex], tail) > 0) {
               storage[parentIndex] = tail;
               break;
            }

            // shift smallest child up the heap, continue percolation from its old index
            storage[parentIndex] = storage[smallestChildIndex];
            parentIndex = smallestChildIndex;
         }
         return true;
      }

      public TItem Dequeue() {
         TItem result;
         if (!TryDequeue(out result))
            throw new InvalidOperationException();
         return result;
      }

      public PriorityQueue<TItem> Copy() {
         var copy = new PriorityQueue<TItem>();
         copy.storage = new TItem[storage.Length];
         Array.Copy(storage, copy.storage, size);
         copy.size = size;
         return copy;
      }

      public IEnumerator<TItem> GetEnumerator() {
         var clone = new PriorityQueue<TItem>(comparer);
         clone.storage = new TItem[storage.Length];
         clone.size = size;
         for (var i = 0; i < size; i++) {
            clone.storage[i] = storage[i];
         }
         while (!clone.IsEmpty) {
            yield return clone.Dequeue();
         }
      }

      IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

      private void Validate() {
         if (IsEmpty || DisableValidation) return;

         var s = new Stack<int>();
         s.Push(0);

         while (s.Count > 0) {
            var current = s.Pop();
            int childrenStartIndexInclusive, childrenEndIndexExclusive;
            ComputeChildrenIndices(current, out childrenStartIndexInclusive, out childrenEndIndexExclusive);

            for (int childIndex = childrenStartIndexInclusive; childIndex < childrenEndIndexExclusive; childIndex++) {
               s.Push(childIndex);
               if (comparer.Compare(storage[current], storage[childIndex]) > 0) {
                  throw new InvalidOperationException("Priority Queue - Heap breaks invariant!");
               }
            }
         }
      }

      private void ComputeChildrenIndices(int currentIndex, out int childrenStartIndexInclusive, out int childrenEndIndexExclusive) {
         childrenStartIndexInclusive = Math.Min(size, currentIndex * HeapBranchFactor + 1);
         childrenEndIndexExclusive = Math.Min(size, currentIndex * HeapBranchFactor + HeapBranchFactor + 1);
      }

      public struct ComparerProxy<T> : IComparer<T> {
         public Comparison<T> comparison;
         public IComparer<T> comparer;

         public int Compare(T x, T y) {
            if (comparison != null) return comparison(x, y);
            return comparer.Compare(x, y);
         }
      }
   }
}
