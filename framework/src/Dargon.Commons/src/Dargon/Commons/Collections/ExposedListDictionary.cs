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
      public ExposedListDictionary(ExposedListDictionary<TKey, TValue> source) : base(source) {}

      public struct EqualityComparerWrapper : IEqualityComparer<TKey> {
         private readonly EqualityComparer<TKey> inner;
         public EqualityComparerWrapper(EqualityComparer<TKey> inner) {
            this.inner = inner;
         }
         public bool Equals(TKey x, TKey y) => inner.Equals(x, y);
         public int GetHashCode(TKey obj) => inner.GetHashCode(obj);
      }

      public new ExposedListDictionary<TKey, TValue> Clone() => new(this);
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

      public ExposedListDictionary(ExposedListDictionary<TKey, TValue, TKeyEqualityComparer> source) {
         this.keyEqualityComparer = source.keyEqualityComparer;
         this.list = new ExposedArrayList<ExposedKeyValuePair<TKey, TValue>>(source.list);
      }

      public bool TryFindIndex(TKey key, out int index) {
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

      IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
         => list.SL()
                .Map(kvp => new KeyValuePair<TKey, TValue>(kvp.Key, kvp.Value))
                .GetEnumerator();

      IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

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

      public ELDKeyCollection<TKey, TValue, TKeyEqualityComparer> Keys => new(this);
      ICollection<TKey> IDictionary<TKey, TValue>.Keys => Keys;
      IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

      public ELDValueCollection<TKey, TValue, TKeyEqualityComparer> Values => new(this);
      ICollection<TValue> IDictionary<TKey, TValue>.Values => Values;
      IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

      public ELDAtIndexer<TKey, TValue> Entries => new() { Store = list };
      public ExposedListDictionary<TKey, TValue, TKeyEqualityComparer> Clone() => new(this);
   }

   public struct ELDAtIndexer<TKey, TValue> {
      public ExposedArrayList<ExposedKeyValuePair<TKey, TValue>> Store;

      public ref ExposedKeyValuePair<TKey, TValue> this[int i] => ref Store[i];
   }

   public struct ELDKeyCollection<TKey, TValue, TKeyEqualityComparer> : IReadOnlyList<TKey>, ICollection<TKey> where TKeyEqualityComparer : struct, IEqualityComparer<TKey> {
      private readonly ExposedListDictionary<TKey, TValue, TKeyEqualityComparer> dict;

      public ELDKeyCollection(ExposedListDictionary<TKey, TValue, TKeyEqualityComparer> dict) {
         this.dict = dict;
      }

      public int Count => dict.Count;
      public bool IsReadOnly => true;

      public bool Contains(TKey item) => dict.ContainsKey(item);

      public void CopyTo(TKey[] array, int arrayIndex) {
         for (var i = 0; i < dict.list.Count; i++) {
            array[arrayIndex++] = dict.list[i].Key;
         }
      }

      public ref TKey this[int index] => ref dict.list[index].Key;
      TKey IReadOnlyList<TKey>.this[int index] => dict.list[index].Key;

      public StructLinq2<TKey, StructLinqMap<ExposedKeyValuePair<TKey, TValue>, ExposedArrayList<ExposedKeyValuePair<TKey, TValue>>.Enumerator, TKey>> GetEnumerator() => dict.GetEnumerator().SL(default(ExposedKeyValuePair<TKey, TValue>)).Map(x => x.Key);
      IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator() => GetEnumerator().inner;
      IEnumerator IEnumerable.GetEnumerator() => GetEnumerator().inner;

      public void Add(TKey item) => throw new NotSupportedException();
      public void Clear() => throw new NotSupportedException();
      public bool Remove(TKey item) => throw new NotSupportedException();
   }

   public struct ELDValueCollection<TKey, TValue, TKeyEqualityComparer> : IReadOnlyList<TValue>, ICollection<TValue> where TKeyEqualityComparer : struct, IEqualityComparer<TKey> {
      private readonly ExposedListDictionary<TKey, TValue, TKeyEqualityComparer> dict;

      public ELDValueCollection(ExposedListDictionary<TKey, TValue, TKeyEqualityComparer> dict) {
         this.dict = dict;
      }

      public int Count => dict.Count;
      public bool IsReadOnly => true;

      public bool Contains(TValue item) => throw new NotSupportedException();

      public void CopyTo(TValue[] array, int arrayIndex) {
         for (var i = 0; i < dict.list.Count; i++) {
            array[arrayIndex++] = dict.list[i].Value;
         }
      }
      
      public ref TValue this[int index] => ref dict.list[index].Value;
      TValue IReadOnlyList<TValue>.this[int index] => dict.list[index].Value;

      public StructLinq2<TValue, StructLinqMap<ExposedKeyValuePair<TKey, TValue>, ExposedArrayList<ExposedKeyValuePair<TKey, TValue>>.Enumerator, TValue>> GetEnumerator() => dict.GetEnumerator().SL(default(ExposedKeyValuePair<TKey, TValue>)).Map(x => x.Value);
      IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() => GetEnumerator().inner;
      IEnumerator IEnumerable.GetEnumerator() => GetEnumerator().inner;

      public void Add(TValue item) => throw new NotSupportedException();
      public void Clear() => throw new NotSupportedException();
      public bool Remove(TValue item) => throw new NotSupportedException();
   }
}
