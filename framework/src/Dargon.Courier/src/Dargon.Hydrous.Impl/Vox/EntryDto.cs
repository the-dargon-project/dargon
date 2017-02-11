using Dargon.Vox;

namespace Dargon.Hydrous.Impl.Vox {
   [AutoSerializable]
   public class EntryDto<K, V> {
      public K Key { get; set; }
      public V Value { get; set; }
      public bool Exists { get; set; }
   }
}