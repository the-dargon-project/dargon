using System;

namespace Dargon.Vox {
   public class InlineTypeSerializer<T> : ITypeSerializer<T> {
      private readonly Action<IBodyWriter, T> write;
      private readonly Action<IBodyReader, T> read;

      public InlineTypeSerializer(Action<IBodyWriter, T> write, Action<IBodyReader, T> read) {
         this.write = write;
         this.read = read;
      }

      public Type Type => typeof(T);

      public void Serialize(IBodyWriter writer, T source) {
         write(writer, source);
      }

      public void Deserialize(IBodyReader reader, T target) {
         read(reader, target);
      }
   }
}