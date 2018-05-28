namespace Dargon.Vox {
   public class SerializableTypeTypeSerializerProxy<T> : ITypeSerializer<T> where T : ISerializableType {
      public void Serialize(IBodyWriter writer, T source) {
         source.Serialize(writer);
      }

      public void Deserialize(IBodyReader reader, T target) {
         target.Deserialize(reader);
      }
   }
}