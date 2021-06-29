namespace Dargon.Commons.Collections {
   public struct ExposedKeyValuePair<K, V> {
      public K Key;
      public V Value;

      public ExposedKeyValuePair(K key, V value) {
         Key = key;
         Value = value;
      }
   }
}