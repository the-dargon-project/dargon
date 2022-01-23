// #define ENABLE_VERSION_CHECK
// #define ENABLE_INDEXER_RANGE_CHECK

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Dargon.Commons.Collections {
   // Doesn't zero free/cleared slots, exposes internal storage.
   // Ripped from PlayOn.
   public class ExposedArrayList<T> : IList<T>, IReadOnlyList<T> {
      private static bool ShouldZeroOnRemove = RuntimeHelpers.IsReferenceOrContainsReferences<T>();

      public int size = 0;
      public T[] store;

#if ENABLE_VERSION_CHECK
      public int version = 0;
#endif

      public ExposedArrayList() : this(16) { }

      public ExposedArrayList(int capacity) {
         store = new T[capacity];
      }

      public ExposedArrayList(ExposedArrayList<T> source) {
         size = source.size;
         store = source.store.ToArray();
      }

      public ExposedArrayList<T>.Enumerator GetEnumerator() => new ExposedArrayList<T>.Enumerator(this);

      IEnumerator<T> IEnumerable<T>.GetEnumerator() => store.Take(size).GetEnumerator();
      IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

      public void Add(T item) {
         EnsureCapacity(size + 1);
         store[size++] = item;
#if ENABLE_VERSION_CHECK
         version++;
#endif
      }

      public void Add(ref T item) {
         EnsureCapacity(size + 1);
         store[size++] = item;
#if ENABLE_VERSION_CHECK
         version++;
#endif
      }

      public void AddRange(T[] items) {
         AddRange(items, 0, items.Length);
      }

      public void AddRange(T[] items, int offset, int length) {
         EnsureCapacity(size + length);
         Array.Copy(items, offset, store, size, length);
         size += length;
#if ENABLE_VERSION_CHECK
         version++;
#endif
      }

      public void AddRange(Span<T> items) {
         EnsureCapacity(size + items.Length);
         items.TryCopyTo(store.AsSpan(size)).AssertIsTrue();
         size += items.Length;
#if ENABLE_VERSION_CHECK
         version++;
#endif
      }

      public void EnsureCapacity(int sz) {
         if (sz > store.Length) {
            var capacity = store.Length;
            while (capacity < sz) {
               capacity <<= 1;
            }
            var buff = new T[capacity];
            Array.Copy(store, 0, buff, 0, size);
            store = buff;
#if ENABLE_VERSION_CHECK
            version++;
#endif
         }
      }

      public void Clear() => size = 0;

      public bool Contains(T item) => throw new NotSupportedException();
      public void CopyTo(T[] array, int arrayIndex) {
         Buffer.BlockCopy(store, 0, array, arrayIndex, array.Length - arrayIndex);
      }

      public bool Remove(T item) => throw new NotSupportedException();

      public int Count => size;
      public bool IsReadOnly => false;

      public int IndexOf(T item) => throw new NotSupportedException();

      public void Insert(int index, T item) => throw new NotSupportedException();

      public void RemoveAt(int index) {
         if (index + 1 == size) {
            size--;
            if (ShouldZeroOnRemove) {
               store[index] = default;
            }
            return;
         }

         throw new NotSupportedException();
      }

      public ref T this[int index] {
         get {
#if ENABLE_INDEXER_RANGE_CHECK
            if ((uint)index >= (uint)size) {
               throw new ArgumentOutOfRangeException();
            }
#endif
            return ref store[index];
         }
      }

      T IReadOnlyList<T>.this[int index] {
         get {
#if ENABLE_INDEXER_RANGE_CHECK
            if ((uint)index >= (uint)size) {
               throw new ArgumentOutOfRangeException();
            }
#endif
            return store[index];
         }
      }

      T IList<T>.this[int index] {
         get {
#if ENABLE_INDEXER_RANGE_CHECK
            if ((uint)index >= (uint)size) {
               throw new ArgumentOutOfRangeException();
            }
#endif
            return store[index];
         }
         set {
#if ENABLE_INDEXER_RANGE_CHECK
            if ((uint)index >= (uint)size) {
               throw new ArgumentOutOfRangeException();
            }
#endif
            store[index] = value;
         }
      }

      public T[] ToArray() {
         var res = new T[size];
         Array.Copy(store, res, size);
         return res;
      }

      /// <summary>
      /// Returns a span pointing into the internal store with the current list size.
      /// Span reference is broken if the EAL expands (e.g. due to an insert resize)
      /// </summary>
      public Span<T> AsSpan() => store.AsSpan(0, size);

      // basically copied from bcl
      public struct Enumerator : IEnumerator<T>, IDisposable, IEnumerator {
         private ExposedArrayList<T> list;
         private int index;
         private T current;

#if ENABLE_VERSION_CHECK
         private int version;
#endif

         public T Current => this.current;

         object IEnumerator.Current
         {
            get
            {
               if (this.index == 0 || this.index == this.list.Count + 1)
                  throw new IndexOutOfRangeException();
               return (object)this.Current;
            }
         }

         internal Enumerator(ExposedArrayList<T> list) {
            this.list = list;
            this.index = 0;
            this.current = default(T);
#if ENABLE_VERSION_CHECK
            this.version = list.version;
#endif
         }

         public void Dispose() {
         }

         public bool MoveNext() {
            if (
#if ENABLE_VERSION_CHECK
               this.version != list.version || 
#endif
               (uint)this.index >= (uint)list.Count)
               return this.MoveNextRare();
            this.current = list.store[this.index];
            this.index = this.index + 1;
            return true;
         }

         private bool MoveNextRare() {
#if ENABLE_VERSION_CHECK
            if (this.version != this.list.version)
               throw new InvalidOperationException("Exposed Array List modified while reading.");
#endif
            this.index = this.list.Count + 1;
            this.current = default(T);
            return false;
         }

         void IEnumerator.Reset() {
#if ENABLE_VERSION_CHECK
            if (this.version != this.list.version)
               throw new InvalidOperationException("Exposed Array List modified while reading.");
#endif
            this.index = 0;
            this.current = default(T);
         }
      }

      public T[] ShrinkStore() {
         if (store.Length == Count) return store;
         store = ToArray();
         return store;
      }
   }
}
