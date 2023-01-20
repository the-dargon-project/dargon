using System;
using System.IO;
using System.Text;

namespace Dargon.Vox2 {
   public class VoxWriter : IDisposable {
      private BinaryWriter bw;

      public VoxWriter(MemoryStream ms) : this(new BinaryWriter(ms, Encoding.UTF8, true)) { }

      public VoxWriter(BinaryWriter bw) {
         this.bw = bw;
      }

      public BinaryWriter InnerWriter {
         get {
            if (bw == null) throw new ObjectDisposedException(nameof(VoxWriter));
            return bw;
         }
      }

      public void WriteTypeIdBytes(byte[] data) => InnerWriter.Write(data);

      public void WriteRawBytes(byte[] data) => InnerWriter.Write(data);

      public void Dispose() {
         bw?.Dispose();
         bw = null;
      }
   }

   [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
   public class VarIntAttribute : VoxInternalBaseAttribute { }

   public class N : VoxInternalBaseDummyType { }

   public class N<T1> : VoxInternalBaseDummyType { }

   public class N<T1, T2> : VoxInternalBaseDummyType { }

   public class P : VoxInternalBaseDummyType { }

   public class P<T1> : VoxInternalBaseDummyType { }

   public class P<T1, T2> : VoxInternalBaseDummyType { }

   [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
   public class PAttribute : VoxInternalBaseAttribute { }

   [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
   public class NAttribute : VoxInternalBaseAttribute { }

   public interface IVoxSerializer<T> {
      int[] FullTypeId { get; }
      byte[] FullTypeIdBytes { get; }

      bool IsUpdatable { get; }

      void WriteFull(VoxWriter writer, ref T val);
      void WriteRaw(VoxWriter writer, ref T val);

      T ReadFull(VoxReader reader);
      T ReadRaw(VoxReader reader);

      void ReadFullIntoRef(VoxReader reader, ref T val);
      void ReadRawIntoRef(VoxReader reader, ref T val);
   }

   public class VoxReader : IDisposable {
      private BinaryReader br;

      public VoxReader(MemoryStream ms) : this(new BinaryReader(ms, Encoding.UTF8, true)) { }

      public VoxReader(BinaryReader br) {
         this.br = br;
      }

      public BinaryReader InnerReader {
         get {
            if (br == null) throw new ObjectDisposedException(nameof(VoxReader));
            return br;
         }
      }

      public int ReadSimpleTypeId() => InnerReader.ReadVariableInt();

      public void AssertReadTypeIdBytes(byte[] bs) => AssertReadRawBytes(bs);

      public void AssertReadRawBytes(byte[] bs) {
         foreach (var b in bs) {
            if (InnerReader.ReadByte() != b) {
               throw new Exception("Byte sequence mismatch!");
            }
         }
      }

      public T ReadPolymorphic<T>() => throw new NotImplementedException();

      public void Dispose() {
         br?.Dispose();
         br = null;
      }
   }
}