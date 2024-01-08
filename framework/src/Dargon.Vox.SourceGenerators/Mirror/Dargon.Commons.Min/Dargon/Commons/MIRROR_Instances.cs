using System.Collections.Generic;

namespace Dargon.Commons;

public static class Instances {
   public static KeyValuePair<TKey, TValue> PairValue<TKey, TValue>(this TKey key, TValue value) {
      return new KeyValuePair<TKey, TValue>(key, value);
   }

   public static KeyValuePair<TKey, TValue> PairKey<TKey, TValue>(this TValue value, TKey key) {
      return key.PairValue(value);
   }
}