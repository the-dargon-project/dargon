using System;
using System.IO;
using System.Text;

namespace Dargon.Vox2 {
   public interface IPolymorphicSerializationOperations {
      Type ReadFullType(VoxReader reader);
      T ReadPolymorphicFull<T>(VoxReader reader);
      
      void WriteFullType(VoxWriter writer, Type type);
      void WritePolymorphicFull<T>(VoxWriter writer, ref T x);
   }

   public class VoxWriter : IDisposable {
      private BinaryWriter bw;
      private IPolymorphicSerializationOperations polymorphicOpsOrNull;

      public VoxWriter(Stream s, bool leaveOpen = true, IPolymorphicSerializationOperations polymorphicOpsOpt = null) : this(new BinaryWriter(s, Encoding.UTF8, leaveOpen), polymorphicOpsOpt) { }

      public VoxWriter(BinaryWriter bw, IPolymorphicSerializationOperations polymorphicOpsOpt = null) {
         this.bw = bw;
         this.polymorphicOpsOrNull = polymorphicOpsOpt;
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
         polymorphicOpsOrNull.WriteFullType(this, type);
      }

      public void WritePolymorphic<T>(T inst) {
         // if (typeof(T).IsValueType) throw new ArgumentException($"{typeof(T).FullName} is a value type.");
         AssertNonNullPolymorphicSerializer();
         polymorphicOpsOrNull.WritePolymorphicFull(this, ref inst);
      }

      private void AssertNonNullPolymorphicSerializer() {
         if (polymorphicOpsOrNull != null) return;
         throw new InvalidOperationException($"The {nameof(VoxReader)} was not constructed with a polymorphic serializer. Consider using VoxContext.CreateReader(..)");
      }

      public void Dispose() {
         bw?.Dispose();
         bw = null;

         polymorphicOpsOrNull = null;
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


   public class L<T> : VoxInternalBaseDummyType { }

   public class D<T1, T2> : VoxInternalBaseDummyType { }

   public interface IVoxSerializer {
      int SimpleTypeId { get; }
      int[] FullTypeId { get; }
      byte[] FullTypeIdBytes { get; }

      bool IsUpdatable { get; }

      void WriteFullObject(VoxWriter writer, object val);
      object ReadRawAsObject(VoxReader reader);
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
      private IPolymorphicSerializationOperations polymorphicSerializationOperationsOpt;

      public VoxReader(Stream s, bool leaveOpen = true, IPolymorphicSerializationOperations polymorphicSerializationOperationsOpt = null) : this(new BinaryReader(s, Encoding.UTF8, leaveOpen), polymorphicSerializationOperationsOpt) { }

      public VoxReader(BinaryReader br, IPolymorphicSerializationOperations polymorphicSerializationOperationsOpt = null) {
         this.br = br;
         this.polymorphicSerializationOperationsOpt = polymorphicSerializationOperationsOpt;
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

      public Type ReadFullType() {
         AssertNonNullPolymorphicSerializer();
         return polymorphicSerializationOperationsOpt.ReadFullType(this);
      }

      public object ReadPolymorphic() => ReadPolymorphic<object>();

      public T ReadPolymorphic<T>() {
         // if (typeof(T).IsValueType) throw new ArgumentException($"{typeof(T).FullName} is a value type.");
         AssertNonNullPolymorphicSerializer();
         return polymorphicSerializationOperationsOpt.ReadPolymorphicFull<T>(this);
      }

      private void AssertNonNullPolymorphicSerializer() {
         if (polymorphicSerializationOperationsOpt != null) return;
         throw new InvalidOperationException($"The {nameof(VoxReader)} was not constructed with a polymorphic serializer. Consider using VoxContext.CreateReader(..)");
      }

      public void Dispose() {
         br?.Dispose();
         br = null;

         polymorphicSerializationOperationsOpt = null;
      }
   }
}