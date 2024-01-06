using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using Dargon.Commons;
using Dargon.Commons.Exceptions;

namespace Dargon.Commons.Collections {
   public class ConcurrentSetMin<T> : IReadOnlyCollection<T> { // IReadOnlySet<T>
      ConcurrentDictionary<T, byte> storage;

      public ConcurrentSetMin()
      {
         storage = new ConcurrentDictionary<T, byte>();
      }

      public ConcurrentSetMin(IEnumerable<T> collection)
      {
         storage = new ConcurrentDictionary<T, byte>(collection.Select(_ => new KeyValuePair<T, byte>(_, 0)));
      }

      public ConcurrentSetMin(IEqualityComparer<T> comparer)
      {
         storage = new ConcurrentDictionary<T, byte>(comparer);
      }

      public ConcurrentSetMin(IEnumerable<T> collection, IEqualityComparer<T> comparer)
      {
         storage = new ConcurrentDictionary<T, byte>(collection.Select(_ => new KeyValuePair<T, byte>(_, 0)), comparer);
      }

      public ConcurrentSetMin(int concurrencyLevel, int capacity)
      {
         storage = new ConcurrentDictionary<T, byte>(concurrencyLevel, capacity);
      }

      public ConcurrentSetMin(int concurrencyLevel, IEnumerable<T> collection, IEqualityComparer<T> comparer)
      {
         storage = new ConcurrentDictionary<T, byte>(concurrencyLevel, collection.Select(_ => new KeyValuePair<T, byte>(_, 0)), comparer);
      }

      public ConcurrentSetMin(int concurrencyLevel, int capacity, IEqualityComparer<T> comparer)
      {
         storage = new ConcurrentDictionary<T, byte>(concurrencyLevel, capacity, comparer);
      }

      public int Count { get { return storage.Count; } }

      public bool IsEmpty { get { return storage.IsEmpty; } }

      public void Clear()
      {
         storage.Clear();
      }

      public bool Contains(T item)
      {
         return storage.ContainsKey(item);
      }

      public bool TryAdd(T item) {
         return storage.TryAdd(item, 0);
      }

      public void AddOrThrow(T item) {
         storage.AddOrThrow(item, (byte)0);
      }

      public bool TryRemove(T item)
      {
         byte dontCare;
         return storage.TryRemove(item, out dontCare);
      }

      public void RemoveOrThrow(T item) {
         storage.RemoveOrThrow(item, (byte)0);
      }

      public bool SetEquals(IEnumerable<T> other) {
         var otherSet = new HashSet<T>(other);
         var otherInitialCount = otherSet.Count;
         var thisCount = 0;
         foreach (var element in this) {
            otherSet.Remove(element);
            thisCount++;
         }
         return otherSet.Count == 0 && thisCount == otherInitialCount;
      }

      public void CopyTo(T[] array) {
         this.CopyTo(array, 0, this.Count);
      }

      public void CopyTo(T[] array, int arrayIndex)
      {
         this.CopyTo(array, arrayIndex, this.Count);
      }

      public void CopyTo(T[] array, int arrayIndex, int count) {
         if (array == null) {
            throw new ArgumentNullException("array");
         } else if (arrayIndex < 0) {
            throw new ArgumentOutOfRangeException("arrayIndex");
         } else if (arrayIndex + count > this.Count) {
            throw new ArgumentException("arrayIndex + count > Count");
         }

         foreach (KeyValuePair<T, byte> pair in storage) {
            array[arrayIndex++] = pair.Key;
            count--;

            if (count == 0)
               break;
         }
      }

      IEnumerator<T> IEnumerable<T>.GetEnumerator()
      {
         return storage.Keys.GetEnumerator();
      }

      IEnumerator IEnumerable.GetEnumerator()
      {
         return storage.Keys.GetEnumerator();
      }

      public bool IsProperSubsetOf(IEnumerable<T> other) {
         throw new NotYetImplementedException();
      }

      public bool IsProperSupersetOf(IEnumerable<T> other) {
         throw new NotYetImplementedException();
      }

      public bool IsSubsetOf(IEnumerable<T> other) {
         throw new NotYetImplementedException();
      }

      public bool IsSupersetOf(IEnumerable<T> other) {
         throw new NotYetImplementedException();
      }

      public bool Overlaps(IEnumerable<T> other) {
         throw new NotYetImplementedException();
      }
   }
}