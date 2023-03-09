using System;
using System.IO;
using System.Text;

namespace Dargon.Vox2 {
   public interface IPolymorphicSerializer {
      Type ReadFullType(VoxReader reader);
      T ReadPolymorphicFull<T>(VoxReader reader);
      
      void WriteFullType(VoxWriter writer, Type type);
      void WritePolymorphicFull<T>(VoxWriter writer, ref T x);
   }

   public class VoxWriter : IDisposable {
      private BinaryWriter bw;
      private IPolymorphicSerializer polymorphicOps;

      public VoxWriter(Stream s, IPolymorphicSerializer polymorphicOps, bool leaveOpen = true) : this(new BinaryWriter(s, Encoding.UTF8, leaveOpen), polymorphicOps) { }

      public VoxWriter(BinaryWriter bw, IPolymorphicSerializer polymorphicOps) {
         this.bw = bw;
         this.polymorphicOps = polymorphicOps;
      }

      public BinaryWriter InnerWriter {
         get {
            if (bw == null) throw new ObjectDisposedException(nameof(VoxWriter));
            return bw;
         }
      }

      public void WriteTypeIdBytes(byte[] data) => InnerWriter.Write(data);

      public void WriteRawBytes(byte[] data) => InnerWriter.Write(data);

      public void WriteFullType(Type type) {
         AssertNonNullPolymorphicSerializer();
         polymorphicOps.WriteFullType(this, type);
      }

      public void WritePolymorphic<T>(T inst) {
         // if (typeof(T).IsValueType) throw new ArgumentException($"{typeof(T).FullName} is a value type.");
         AssertNonNullPolymorphicSerializer();
         polymorphicOps.WritePolymorphicFull(this, ref inst);
      }

      private void AssertNonNullPolymorphicSerializer() {
         if (polymorphicOps != null) return;
         throw new InvalidOperationException($"The {nameof(VoxReader)} was not constructed with a polymorphic serializer. Consider using VoxContext.CreateReader(..)");
      }

      public void Dispose() {
         bw?.Dispose();
         bw = null;

         polymorphicOps = null;
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

   // List
   public class L<T> : VoxInternalBaseDummyType { }

   // Dict
   public class D<T1, T2> : VoxInternalBaseDummyType { }

   public interface IVoxSerializer {
      int SimpleTypeId { get; }
      int[] FullTypeId { get; }
      byte[] FullTypeIdBytes { get; }

      bool IsUpdatable { get; }

      void WriteRawObject(VoxWriter writer, object val);
      object ReadRawObject(VoxReader reader);
   }

   public interface IVoxSerializer<T> : IVoxSerializer {
      void WriteFull(VoxWriter writer, ref T val);
      void WriteRaw(VoxWriter writer, ref T val);

      T ReadFull(VoxReader reader);
      T ReadRaw(VoxReader reader);

      void ReadFullIntoRef(VoxReader reader, ref T val);
      void ReadRawIntoRef(VoxReader reader, ref T val);
   }

   public class VoxReader : IDisposable {
      private BinaryReader br;
      private IPolymorphicSerializer polymorphicSerializer;

      public VoxReader(Stream s, IPolymorphicSerializer polymorphicSerializer, bool leaveOpen = true) : this(new BinaryReader(s, Encoding.UTF8, leaveOpen), polymorphicSerializer) { }

      public VoxReader(BinaryReader br, IPolymorphicSerializer polymorphicSerializer) {
         this.br = br;
         this.polymorphicSerializer = polymorphicSerializer;
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
         for (var i = 0; i < bs.Length; i++) {
            var x = InnerReader.ReadByte();
            if (x != bs[i]) {
               throw new Exception("Byte sequence mismatch!");
            }
         }
      }

      public Type ReadFullType() {
         AssertNonNullPolymorphicSerializer();
         return polymorphicSerializer.ReadFullType(this);
      }

      public object ReadPolymorphic() => ReadPolymorphic<object>();

      public T ReadPolymorphic<T>() {
         // if (typeof(T).IsValueType) throw new ArgumentException($"{typeof(T).FullName} is a value type.");
         AssertNonNullPolymorphicSerializer();
         return polymorphicSerializer.ReadPolymorphicFull<T>(this);
      }

      private void AssertNonNullPolymorphicSerializer() {
         if (polymorphicSerializer != null) return;
         throw new InvalidOperationException($"The {nameof(VoxReader)} was not constructed with a polymorphic serializer. Consider using VoxContext.CreateReader(..)");
      }

      public void Dispose() {
         br?.Dispose();
         br = null;

         polymorphicSerializer = null;
      }
   }
}