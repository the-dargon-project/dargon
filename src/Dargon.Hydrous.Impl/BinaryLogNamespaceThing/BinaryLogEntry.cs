using Dargon.Vox;

namespace Dargon.Hydrous.Impl.BinaryLogNamespaceThing {
   [AutoSerializable]
   public class BinaryLogEntry {
      public BinaryLogEntry() { }

      public BinaryLogEntry(int id, object data) {
         Id = id;
         Data = data;
      }

      public int Id { get; set; }
      public object Data { get; set; }
   }
}