﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Dargon.Commons.Collections
{
   /// <summary>
   /// A dictionary object that allows rapid hash lookups using keys, but also
   /// maintains the key insertion order so that values can be retrieved by
   /// key index.
   /// via: http://stackoverflow.com/questions/2629027/no-generic-implementation-of-ordereddictionary
   /// </summary>
   public class OrderedDictionary<TKey, TValue> : IOrderedDictionary<TKey, TValue>
   {

      #region Fields/Properties

      private KeyedCollection2<TKey, KeyValuePair<TKey, TValue>> _keyedCollection;

      public TValue this[int index]
      {
         get
         {
            if (index < 0 || index >= _keyedCollection.Count)
            {
               throw new ArgumentException(String.Format("The index is outside the bounds of the dictionary: {0}", index));
            }
            return _keyedCollection[index].Value;
         }
         set
         {
            if (index < 0 || index >= _keyedCollection.Count)
            {
               throw new ArgumentException(String.Format("The index is outside the bounds of the dictionary: {0}", index));
            }
            var kvp = new KeyValuePair<TKey, TValue>(_keyedCollection[index].Key, value);
            _keyedCollection[index] = kvp;
         }
      }

      public TValue this[TKey key]
      {
         get
         {
            if (_keyedCollection.Contains(key) == false)
            {
               throw new ArgumentException(String.Format("The given key is not present in the dictionary: {0}", key));
            }
            var kvp = _keyedCollection[key];
            return kvp.Value;
         }
         set
         {
            var kvp = new KeyValuePair<TKey, TValue>(key, value);
            var idx = IndexOf(key);
            if (idx > -1)
            {
               _keyedCollection[idx] = kvp;
            }
            else
            {
               _keyedCollection.Add(kvp);
            }
         }
      }

      public int Count
      {
         get { return _keyedCollection.Count; }
      }

      public ICollection<TKey> Keys
      {
         get
         {
            return _keyedCollection.Select(x => x.Key).ToList();
         }
      }

      public ICollection<TValue> Values
      {
         get
         {
            return _keyedCollection.Select(x => x.Value).ToList();
         }
      }

      public IEqualityComparer<TKey> Comparer
      {
         get;
         private set;
      }

      #endregion

      #region Constructors

      public OrderedDictionary()
      {
         Initialize();
      }

      public OrderedDictionary(IEqualityComparer<TKey> comparer)
      {
         Initialize(comparer);
      }

      public OrderedDictionary(IOrderedDictionary<TKey, TValue> dictionary)
      {
         Initialize();
         foreach (KeyValuePair<TKey, TValue> pair in dictionary)
         {
            _keyedCollection.Add(pair);
         }
      }

      public OrderedDictionary(IOrderedDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer)
      {
         Initialize(comparer);
         foreach (KeyValuePair<TKey, TValue> pair in dictionary)
         {
            _keyedCollection.Add(pair);
         }
      }

      #endregion

      #region Methods

      private void Initialize(IEqualityComparer<TKey> comparer = null)
      {
         this.Comparer = comparer;
         if (comparer != null)
         {
            _keyedCollection = new KeyedCollection2<TKey, KeyValuePair<TKey, TValue>>(x => x.Key, comparer);
         }
         else
         {
            _keyedCollection = new KeyedCollection2<TKey, KeyValuePair<TKey, TValue>>(x => x.Key);
         }
      }

      public void Add(TKey key, TValue value)
      {
         _keyedCollection.Add(new KeyValuePair<TKey, TValue>(key, value));
      }

      public void Clear()
      {
         _keyedCollection.Clear();
      }

      public void Insert(int index, TKey key, TValue value)
      {
         _keyedCollection.Insert(index, new KeyValuePair<TKey, TValue>(key, value));
      }

      public int IndexOf(TKey key)
      {
         if (_keyedCollection.Contains(key))
         {
            return _keyedCollection.IndexOf(_keyedCollection[key]);
         }
         else
         {
            return -1;
         }
      }

      public bool ContainsValue(TValue value)
      {
         return this.Values.Contains(value);
      }

      public bool ContainsValue(TValue value, IEqualityComparer<TValue> comparer)
      {
         return this.Values.Contains(value, comparer);
      }

      public bool ContainsKey(TKey key)
      {
         return _keyedCollection.Contains(key);
      }

      public KeyValuePair<TKey, TValue> GetItem(int index)
      {
         if (index < 0 || index >= _keyedCollection.Count)
         {
            throw new ArgumentException(String.Format("The index was outside the bounds of the dictionary: {0}", index));
         }
         return _keyedCollection[index];
      }

      public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
      {
         return _keyedCollection.GetEnumerator();
      }

      public bool Remove(TKey key)
      {
         return _keyedCollection.Remove(key);
      }

      public void RemoveAt(int index)
      {
         if (index < 0 || index >= _keyedCollection.Count)
         {
            throw new ArgumentException(String.Format("The index was outside the bounds of the dictionary: {0}", index));
         }
         _keyedCollection.RemoveAt(index);
      }

      public bool TryGetValue(TKey key, out TValue value)
      {
         if (_keyedCollection.Contains(key))
         {
            value = _keyedCollection[key].Value;
            return true;
         }
         else
         {
            value = default(TValue);
            return false;
         }
      }

      #endregion

      #region sorting
      public void SortKeys()
      {
         _keyedCollection.SortByKeys();
      }

      public void SortKeys(IComparer<TKey> comparer)
      {
         _keyedCollection.SortByKeys(comparer);
      }

      public void SortKeys(Comparison<TKey> comparison)
      {
         _keyedCollection.SortByKeys(comparison);
      }

      public void SortValues()
      {
         var comparer = Comparer<TValue>.Default;
         SortValues(comparer);
      }

      public void SortValues(IComparer<TValue> comparer)
      {
         _keyedCollection.Sort((x, y) => comparer.Compare(x.Value, y.Value));
      }

      public void SortValues(Comparison<TValue> comparison)
      {
         _keyedCollection.Sort((x, y) => comparison(x.Value, y.Value));
      }
      #endregion

      #region IDictionary<TKey, TValue>

      void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
      {
         Add(key, value);
      }

      bool IDictionary<TKey, TValue>.ContainsKey(TKey key)
      {
         return ContainsKey(key);
      }

      ICollection<TKey> IDictionary<TKey, TValue>.Keys
      {
         get { return Keys; }
      }

      bool IDictionary<TKey, TValue>.Remove(TKey key)
      {
         return Remove(key);
      }

      bool IDictionary<TKey, TValue>.TryGetValue(TKey key, out TValue value)
      {
         return TryGetValue(key, out value);
      }

      ICollection<TValue> IDictionary<TKey, TValue>.Values
      {
         get { return Values; }
      }

      TValue IDictionary<TKey, TValue>.this[TKey key]
      {
         get
         {
            return this[key];
         }
         set
         {
            this[key] = value;
         }
      }

      #endregion

      #region ICollection<KeyValuePair<TKey, TValue>>

      void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
      {
         _keyedCollection.Add(item);
      }

      void ICollection<KeyValuePair<TKey, TValue>>.Clear()
      {
         _keyedCollection.Clear();
      }

      bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
      {
         return _keyedCollection.Contains(item);
      }

      void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
      {
         _keyedCollection.CopyTo(array, arrayIndex);
      }

      int ICollection<KeyValuePair<TKey, TValue>>.Count
      {
         get { return _keyedCollection.Count; }
      }

      bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
      {
         get { return false; }
      }

      bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
      {
         return _keyedCollection.Remove(item);
      }

      #endregion

      #region IEnumerable<KeyValuePair<TKey, TValue>>

      IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
      {
         return GetEnumerator();
      }

      #endregion

      #region IEnumerable

      IEnumerator IEnumerable.GetEnumerator()
      {
         return GetEnumerator();
      }

      #endregion
   }

   public class KeyedCollection2<TKey, TItem> : KeyedCollection<TKey, TItem>
   {
      private const string DelegateNullExceptionMessage = "Delegate passed cannot be null";
      private Func<TItem, TKey> _getKeyForItemDelegate;

      public KeyedCollection2(Func<TItem, TKey> getKeyForItemDelegate)
         : base()
      {
         if (getKeyForItemDelegate == null) throw new ArgumentNullException(DelegateNullExceptionMessage);
         _getKeyForItemDelegate = getKeyForItemDelegate;
      }

      public KeyedCollection2(Func<TItem, TKey> getKeyForItemDelegate, IEqualityComparer<TKey> comparer)
         : base(comparer)
      {
         if (getKeyForItemDelegate == null) throw new ArgumentNullException(DelegateNullExceptionMessage);
         _getKeyForItemDelegate = getKeyForItemDelegate;
      }

      protected override TKey GetKeyForItem(TItem item)
      {
         return _getKeyForItemDelegate(item);
      }

      public void SortByKeys()
      {
         var comparer = Comparer<TKey>.Default;
         SortByKeys(comparer);
      }

      public void SortByKeys(IComparer<TKey> keyComparer)
      {
         var comparer = new Comparer2<TItem>((x, y) => keyComparer.Compare(GetKeyForItem(x), GetKeyForItem(y)));
         Sort(comparer);
      }

      public void SortByKeys(Comparison<TKey> keyComparison)
      {
         var comparer = new Comparer2<TItem>((x, y) => keyComparison(GetKeyForItem(x), GetKeyForItem(y)));
         Sort(comparer);
      }

      public void Sort()
      {
         var comparer = Comparer<TItem>.Default;
         Sort(comparer);
      }

      public void Sort(Comparison<TItem> comparison)
      {
         var newComparer = new Comparer2<TItem>((x, y) => comparison(x, y));
         Sort(newComparer);
      }

      public void Sort(IComparer<TItem> comparer)
      {
         List<TItem> list = base.Items as List<TItem>;
         if (list != null)
         {
            list.Sort(comparer);
         }
      }
   }

   public class Comparer2<T> : Comparer<T>
   {
      //private readonly Func<T, T, int> _compareFunction;
      private readonly Comparison<T> _compareFunction;

      #region Constructors

      public Comparer2(Comparison<T> comparison)
      {
         if (comparison == null) throw new ArgumentNullException("comparison");
         _compareFunction = comparison;
      }

      #endregion

      public override int Compare(T arg1, T arg2)
      {
         return _compareFunction(arg1, arg2);
      }
   }

   public class DictionaryEnumerator<TKey, TValue> : IDictionaryEnumerator, IDisposable
   {
      readonly IEnumerator<KeyValuePair<TKey, TValue>> impl;
      public void Dispose() { impl.Dispose(); }
      public DictionaryEnumerator(IDictionary<TKey, TValue> value)
      {
         this.impl = value.GetEnumerator();
      }
      public void Reset() { impl.Reset(); }
      public bool MoveNext() { return impl.MoveNext(); }
      public DictionaryEntry Entry
      {
         get
         {
            var pair = impl.Current;
            return new DictionaryEntry(pair.Key, pair.Value);
         }
      }
      public object Key { get { return impl.Current.Key; } }
      public object Value { get { return impl.Current.Value; } }
      public object Current { get { return Entry; } }
   }
}
