using Dargon.Courier;

namespace Dargon.Hydrous.Impl.Store {
   public class PendingUpdate<K, V> {
      public Entry<K, V> Base { get; set; }
      public Entry<K, V> Updated { get; set; }
   }
}