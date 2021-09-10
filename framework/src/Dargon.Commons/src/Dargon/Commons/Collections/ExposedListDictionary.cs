using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Dargon.Commons.Exceptions;

namespace Dargon.Commons.Collections {
   
   public class ExposedListDictionary<TKey, TValue> : ExposedListDictionary<TKey, TValue, ExposedListDictionary<TKey, TValue>.EqualityComparerWrapper> {
      public ExposedListDictionary() : base(new EqualityComparerWrapper(EqualityComparer<TKey>.Default)) { }
      public ExposedListDictionary(int capacity) : base(capacity, new EqualityComparerWrapper(EqualityComparer<TKey>.Default)) { }
      public ExposedListDictionary(EqualityComparer<TKey> equalityComparer) : base(new EqualityComparerWrapper(equalityComparer)) { }
      public ExposedListDictionary(int capacity, EqualityComparer<TKey> equalityComparer) : base(capacity, new EqualityComparerWrapper(equalityComparer)) { }

      public struct EqualityComparerWrapper : IEqualityComparer<TKey> {
         private readonly EqualityComparer<TKey> inner;
         public EqualityComparerWrapper(EqualityComparer<TKey> inner) {
            this.inner = inner;
         }
         public bool Equals(TKey x, TKey y) => inner.Equals(x, y);
         public int GetHashCode(TKey obj) => inner.GetHashCode(obj);
      }
   }

   public class ExposedListDictionary<TKey, TValue, TKeyEqualityComparer> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue> where TKeyEqualityComparer : struct, IEqualityComparer<TKey> {
      private readonly TKeyEqualityComparer keyEqualityComparer;
      public readonly ExposedArrayList<ExposedKeyValuePair<TKey, TValue>> list;
      private const int kDefaultCapacity = 4;

      public ExposedListDictionary(TKeyEqualityComparer keyEqualityComparer) : this(kDefaultCapacity, keyEqualityComparer) { }

      public ExposedListDictionary(int capacity, TKeyEqualityComparer keyEqualityComparer) {
         this.keyEqualityComparer = keyEqualityComparer;
         this.list = new ExposedArrayList<ExposedKeyValuePair<TKey, TValue>>(capacity);
      }

      private bool TryFindIndex(TKey key, out int index) {
         for (var i = 0; i < list.Count; i++) {
            if (keyEqualityComparer.Equals(key, list.store[i].Key)) {
               index = i;
               return true;
            }
         }

         index = -1;
         return false;
      }

      public TValue GetOrThrow(TKey key) {
         if (!TryFindIndex(key, out var index)) {
            throw new KeyNotFoundException();
         }

         return list.store[index].Value;
      }
      
      public ref TValue GetRef(TKey key) {
         if (!TryFindIndex(key, out var index)) {
            throw new KeyNotFoundException();
         }

         return ref list.store[index].Value;
      }
      public void AddOrThrow(KeyValuePair<TKey, TValue> item) {
         if (TryFindIndex(item.Key, out var idx)) {
            throw new ArgumentException("Key already existed!");
         }

         list.Add(new ExposedKeyValuePair<TKey, TValue>(item.Key, item.Value));
      }

      /// <summary>
      /// Amends the given entry to the listdictionary, foregoing duplicate key checks.
      /// </summary>
      public void AddEntryOfUniqueKeyUnsafe(TKey key, TValue value)
         => list.Add(new ExposedKeyValuePair<TKey, TValue>(key, value));

      public bool AddOrUpdate(TKey key, TValue value) {
         for (var i = 0; i < list.Count; i++) {
            if (keyEqualityComparer.Equals(key, list[i].Key)) {
               list.store[i].Value = value;
               return false;
            }
         }

         list.Add(new ExposedKeyValuePair<TKey, TValue>(key, value));
         return true;
      }

      public ExposedArrayList<ExposedKeyValuePair<TKey, TValue>>.Enumerator GetEnumerator() => list.GetEnumerator();

      IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => throw new NotYetImplementedException();

      IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

      void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => AddOrThrow(item);

      public void Clear() { list.Clear(); }
      
      public bool Contains(KeyValuePair<TKey, TValue> item) { return list.Contains(new ExposedKeyValuePair<TKey, TValue>(item.Key, item.Value)); }

      public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
         for (var i = 0; i < list.size; i++, arrayIndex++) {
            ref var ekvp = ref list.store[i];
            array[arrayIndex] = new KeyValuePair<TKey, TValue>(ekvp.Key, ekvp.Value);
         }
      }

      public bool Remove(KeyValuePair<TKey, TValue> item) => throw new NotImplementedException();
      
      public int Count => list.Count;

      bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

      void IDictionary<TKey, TValue>.Add(TKey key, TValue value) => AddOrThrow(key, value);

      public bool ContainsKey(TKey key) => TryFindIndex(key, out _);
      public bool Remove(TKey key) => RemoveKey(key);

      public void AddOrThrow(TKey key, TValue value) => AddOrThrow(new KeyValuePair<TKey, TValue>(key, value));

      public void RemoveKeyOrThrow(TKey key) => RemoveKey(key).AssertIsTrue();
      
      public TValue GetAndRemoveKeyOrThrow(TKey key) {
         RemoveKey(key, out var value).AssertIsTrue();
         return value;
      }

      public bool RemoveKey(TKey key) {
         if (TryFindIndex(key, out var removedIndex)) {
            var lastItemIndex = list.size - 1;
            if (removedIndex != lastItemIndex) {
               list.store[removedIndex] = list.store[lastItemIndex];
            }

            list.store[lastItemIndex] = default;
            list.size--;
            return true;
         }
         
         return false;
      }

      public bool RemoveKey(TKey key, out TValue value) {
         if (TryFindIndex(key, out var removedIndex)) {
            value = list[removedIndex].Value;

            var lastItemIndex = list.size - 1;
            if (removedIndex != lastItemIndex) {
               list.store[removedIndex] = list.store[lastItemIndex];
            }

            list.store[lastItemIndex] = default;
            list.size--;
            return true;
         }

         Unsafe.SkipInit(out value);
         return false;
      }

      public bool TryGetValue(TKey key, out TValue value) {
         if (TryFindIndex(key, out var idx)) {
            value = list.store[idx].Value;
            return true;
         }

         value = default;
         return false;
      }

      public TValue this[TKey key] { 
         get => GetOrThrow(key);
         set => AddOrUpdate(key, value);
      }

      public ICollection<TKey> Keys { get { return Arrays.Create(list.Count, i => list[i].Key); } }
      IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;
      public ICollection<TValue> Values { get { return Arrays.Create(list.Count, i => list[i].Value); } }
      IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;
   }
}
