using System;
using System.Collections.Generic;
using System.Numerics;

namespace Dargon.Commons.Collections;

public class ExposedSortedListCounter<TKey, TCount> 
   where TCount : struct, IBinaryInteger<TCount> 
   where TKey : IComparable<TKey> {
   public readonly ExposedListDictionary<TKey, TCount> inner = new();

   public int Count => inner.Count;

   public TCount PreIncrement(TKey key) {
      var l = inner.list.store;
      if (inner.TryFindIndex(key, out var i)) {
         var res = ++l[i].Value;
         if (TCount.IsZero(res)) throw new OverflowException();
         return res;
      } else {
         inner.AddOrThrow(key, TCount.CreateTruncating(1));

         // bubble backward OK for small collections
         for (var idx = inner.Count - 2; idx >= 0; idx--) {
            if (l[idx].Key.CompareTo(l[idx + 1].Key) > 0) {
               (l[idx], l[idx + 1]) = (l[idx + 1], l[idx]);
            }
         }

         return TCount.CreateTruncating(1);
      }
   }

   public TCount PreDecrement(TKey key) {
      if (inner.TryFindIndex(key, out var i)) {
         var newValue = --inner.list[i].Value;
         if (newValue == TCount.Zero) {
            inner.list.RemoveAt(i);
         }
         return newValue;
      }

      throw new KeyNotFoundException($"Not contained in {GetType().Name}: {key}");
   }

   public ExposedKeyValuePair<TKey, TCount> Min() => inner.list[0];
   public ExposedKeyValuePair<TKey, TCount> Max() => inner.list[inner.list.size - 1];
}