namespace Dargon.Vox {
   public interface IBodyWriter {
      void Write<T>(T val);
      void Write(byte[] buffer, int offset, int length);
   }

   public interface IBodyReader {
      T Read<T>();
   }

   public interface ITypeSerializer { }

   public interface ITypeSerializer<T> : ITypeSerializer {
      void Serialize(IBodyWriter writer, T source);
      void Deserialize(IBodyReader reader, T target);
   }

   public interface ISerializableType {
      void Serialize(IBodyWriter writer);
      void Deserialize(IBodyReader reader);
   }
}
