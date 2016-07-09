using Dargon.Courier.Store;
using Dargon.Vox;

namespace Dargon.Courier {
   [AutoSerializable]
   public class Entry<K, V> : IEntry<K, V> {
      private V value;

      [PrimaryKey] public K Key { get; private set; }
      public V Value { get { return value; } set { SetValue(value); } }
      public bool Exists { get; private set; }
      public bool IsDirty { get; set; }

      private void SetValue(V newValue) {
         value = newValue;
         IsDirty = true;
         Exists = true;
      }

      public override string ToString() => $"[Entry K={Key}, V={Value}]";
      public static Entry<K, V> CreateNonexistant(K key) => new Entry<K, V> { Key = key };
      public static Entry<K, V> CreateExistantWithValue(K key, V value) => new Entry<K, V> { Key = key, Value = value, IsDirty = false };
   }
}