// #define ENABLE_VERSION_CHECK

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Dargon.Commons.Collections {
   // Doesn't zero free/cleared slots, exposes internal storage.
   // Ripped from PlayOn.
   public class ExposedArrayList<T> : IList<T>, IReadOnlyList<T> {
      public int size = 0;
      public T[] store;

#if ENABLE_VERSION_CHECK
      public int version = 0;
#endif

      public ExposedArrayList() : this(16) { }

      public ExposedArrayList(int capacity) {
         store = new T[capacity];
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

      public void RemoveAt(int index) => throw new NotSupportedException();

      public T this[int index] {
         get {
            // if ((uint)index >= (uint)size) {
            //    throw new ArgumentOutOfRangeException();
            // }
            return store[index];
         }
         set {
            // if ((uint)index >= (uint)size) {
            //    throw new ArgumentOutOfRangeException();
            // }
            store[index] = value;
         }
      }

      public T[] ToArray() {
         var res = new T[size];
         Array.Copy(store, res, size);
         return res;
      }

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
   }
}
