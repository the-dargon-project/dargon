// #define ENABLE_VERSION_CHECK

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Commons.Exceptions;

namespace Dargon.Commons.Collections {
   public class ExposedArrayDeque<T> : IEnumerable<T> {
      // below, head and tail indices correspond to valid (active) indices in the store.
      // in the case where numItems is 0, headIndex can be any valid index.
      public T[] store;
      public int headIndex;
      public int size;

#if ENABLE_VERSION_CHECK
      public int version;
#endif

      public ExposedArrayDeque(int capacity = 16) {
         capacity.AssertIsGreaterThan(0);
         store = new T[capacity];
      }

      public int Capacity => store.Length;

      public bool LoopsPastEnd => headIndex > 0 && headIndex + size > store.Length;

      public int Size => size;

      public void EnsureCapacity(int n) {
         if (store.Length >= n) return;

         var newCapacity = (int)BitMath.CeilingPow2((uint)n);
         var newStore = new T[newCapacity];
         var ni = 0;
         foreach (var x in this) {
            newStore[ni++] = x;
         }

         store = newStore;
         headIndex = 0;
#if ENABLE_VERSION_CHECK
         version++;
#endif
      }

      // e.g. [0, 1, 2, 3] becomes [1, 2, 3, 0]
      public void RotateLeft(int n = 1) {
         n.AssertIsGreaterThanOrEqualTo(0);
         size.AssertEquals(store.Length);
         headIndex += n;
         while (headIndex >= store.Length) headIndex -= store.Length;
#if ENABLE_VERSION_CHECK
         version++;
#endif
      }

      // e.g. [0, 1, 2, 3] becomes [3, 0, 1, 2]
      public void RotateRight(int n = 1) {
         n.AssertIsGreaterThanOrEqualTo(0);
         size.AssertEquals(store.Length);
         headIndex -= n;
         while (headIndex < 0) headIndex += store.Length;
#if ENABLE_VERSION_CHECK
         version++;
#endif
      }

      public void AddFirst(T item) {
         EnsureCapacity(size + 1);
         if (size == 0) {
            store[headIndex] = item;
            size++;
#if ENABLE_VERSION_CHECK
            version++;
#endif
         } else {
            var i = NormalizePredecessorIndex(headIndex - 1);
            store[i] = item;
            headIndex = i;
            size++;
#if ENABLE_VERSION_CHECK
            version++;
#endif
         }
      }

      public void RemoveFirst() {
         if (size == 0) throw new InvalidOperationException();
         store[headIndex] = default;
         headIndex = NormalizeSuccessorIndex(headIndex + 1);
         size--;
#if ENABLE_VERSION_CHECK
         version++;
#endif
      }

      public void AddLast(T item) {
         EnsureCapacity(size + 1);
         if (size == 0) {
            store[headIndex] = item;
            size++;
#if ENABLE_VERSION_CHECK
            version++;
#endif
         } else {
            var i = NormalizeSuccessorIndex(headIndex + size);
            store[i] = item;
            size++;
#if ENABLE_VERSION_CHECK
            version++;
#endif
         }
      }

      public void RemoveLast() {
         if (size == 0) throw new InvalidOperationException();
         var tailIndex = NormalizeSuccessorIndex(headIndex + size - 1);
         store[tailIndex] = default;
         size--;
#if ENABLE_VERSION_CHECK
         version++;
#endif
      }

      public void Clear() {
         var ni = headIndex;
         for (var i = 0; i < size; i++) {
            store[ni] = default;

            ni++;
            if (ni == store.Length) {
               ni = 0;
            }
         }

         size = 0;
#if ENABLE_VERSION_CHECK
         version++;
#endif
      }

      public ref T this[int i] {
         get {
            i.AssertIsGreaterThanOrEqualTo(0).AssertIsLessThan(size);
            return ref store[NormalizeSuccessorIndex(headIndex + i)];
         }
      }

      private int NormalizePredecessorIndex(int i) => i < 0 ? (i + store.Length) : i;
      private int NormalizeSuccessorIndex(int i) => i >= store.Length ? (i - store.Length) : i;

      public Enumerator GetEnumerator() => new(this);
      IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
      IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

      public struct Enumerator : IEnumerator<T> {
         private readonly ExposedArrayDeque<T> deque;
         private int pastMoveNextCount;
         private int currentIndex;

#if ENABLE_VERSION_CHECK
         private int version;
#endif

         public Enumerator(ExposedArrayDeque<T> deque) {
            this.deque = deque;
            this.pastMoveNextCount = 0;
            this.currentIndex = deque.headIndex - 1;
#if ENABLE_VERSION_CHECK
            this.version = deque.version;
#endif
         }

         public bool MoveNext() {
#if ENABLE_VERSION_CHECK
            if (version != deque.version) {
               throw new InvalidOperationException();
            }
#endif

            if (pastMoveNextCount >= deque.size) {
               currentIndex = -1;
               return false;
            }

            pastMoveNextCount++;
            currentIndex++;
            if (currentIndex == deque.store.Length) {
               currentIndex = 0;
            }

            return true;
         }

         public void Reset() {
            this.pastMoveNextCount = 0;
            this.currentIndex = deque.headIndex - 1;
         }

         public T Current => deque.store[currentIndex];

         object IEnumerator.Current => Current;

         public void Dispose() { }
      }
   }
}
