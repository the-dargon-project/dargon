using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dargon.Commons {
   public static class CollectionStatics {
      public static IEnumerable<KeyValuePair<TProj, T>> SelectPairKey<T, TProj>(this IEnumerable<T> e, Func<T, TProj> proj)
         => e.Select(x => x.PairKey(proj(x)));

      public static IEnumerable<KeyValuePair<T, TProj>> SelectPairValue<T, TProj>(this IEnumerable<T> e, Func<T, TProj> proj)
         => e.Select(x => x.PairValue(proj(x)));

      public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> source, out TKey Key, out TValue Value) {
         Key = source.Key;
         Value = source.Value;
      }

      public static void Deconstruct<TKey, TValue>(this IGrouping<TKey, TValue> source, out TKey Key, out IEnumerable<TValue> Value) {
         Key = source.Key;
         Value = source;
      }
   }
}
