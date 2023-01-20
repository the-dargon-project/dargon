using System.Numerics;
using Dargon.Commons;
using Dargon.Commons.Templating;
using Dargon.Commons.Utilities;

namespace Dargon.Vox2 {
   public class Program {
      static void Main(string[] args) {
         Console.WriteLine("Hello, World!");

         using var ms = new MemoryStream();
         using var vw = new VoxWriter(ms);
         vw.WriteFullSimpleTestType(new SimpleTestType{ i = 10, s = "Hello, World!"});
         ms.Position = 0;
         using var vr = new VoxReader(ms);
         var rt = vr.ReadFullSimpleTestType();
         rt.i.AssertEquals(10);
         rt.s.AssertEquals("Hello, World!");

         // vw.WriteFull(new HodgepodgeMin());
      }
   }

   public abstract class VoxTypes {
      public abstract List<Type> AutoserializedTypes { get; }
      public abstract Dictionary<Type, Type> TypeToCustomSerializers { get; }
      public abstract List<Type> DependencyVoxTypes { get; }
   }

   public class CoreVoxTypes : VoxTypes {
      public override List<Type> AutoserializedTypes { get; } = new();

      public override Dictionary<Type, Type> TypeToCustomSerializers { get; } = new() {
         [typeof(Int32)] = typeof(Int32Serializer),
         [typeof(Guid)] = typeof(GuidSerializer),
         [typeof(String)] = typeof(StringSerializer),
      };

      public override List<Type> DependencyVoxTypes { get; } = new();
   }

   public class TestVoxTypes : VoxTypes {
      public override List<Type> AutoserializedTypes { get; } = new() { typeof(SimpleTestType) };
      public override Dictionary<Type, Type> TypeToCustomSerializers { get; } = new() { };
      public override List<Type> DependencyVoxTypes { get; } = new() { typeof(CoreVoxTypes) };
   }

   /// <summary>Serialize int/enum as variable-size for compactness</summary>
   public class PAttribute<T1> : VoxInternalBaseAttribute { }
   public class PAttribute<T1, T2> : VoxInternalBaseAttribute { }
   public class NAttribute<T1> : VoxInternalBaseAttribute { }
   public class NAttribute<T1, T2> : VoxInternalBaseAttribute { }

   // [VoxType((int)BuiltInVoxTypeIds.ValueTuple0, RedirectToType = typeof(ValueTuple))]
   // [VoxType((int)BuiltInVoxTypeIds.ValueTuple1, RedirectToType = typeof(ValueTuple<>))]
   // [VoxType((int)BuiltInVoxTypeIds.ValueTuple2, RedirectToType = typeof(ValueTuple<,>))]
   // public partial class ValueTuple_VoxTypeRedirects {}

   [VoxType((int)BuiltInVoxTypeIds.ReservedForInternalVoxTest0)]
   public partial class SimpleTestType {
      public int i;
      public string? s;
   }
   
   [VoxType((int)BuiltInVoxTypeIds.ReservedForInternalVoxTest1)]
   public partial class HodgepodgeMin {
      public int Int32 { get; set; }
      public Guid Guid { get; set; }
      public int[] IntArray { get; set; }
      [N<N, N<N<N<P, N<N>>>>>] public Dictionary<int, Dictionary<object, int[]>[][]> DictOfIntToArrayOfArrayOfDictOfStringToIntArray { get; set; }
      // public Type Type { get; set; }
      // public (int, string) Tuple { get; set; }
      // public Vector3 Vector3 { get; set; }
      // public HodgepodgeMin Inner { get; set; }
   }

   public class HodgepodgeDto {
      public bool True { get; set; }
      public bool False { get; set; }
      public sbyte Int8 { get; set; }
      public short Int16 { get; set; }
      public int Int32 { get; set; }
      public long Int64 { get; set; }
      public byte UInt8 { get; set; }
      public ushort UInt16 { get; set; }
      public uint UInt32 { get; set; }
      public ulong UInt64 { get; set; }
      public FileAccess FileAccess { get; set; }
      public string String { get; set; }
      public Guid Guid { get; set; }
      public List<int> IntList { get; set; }
      public string[] StringArray { get; set; }
      public int[] IntPowersArray { get; set; }
      public Dictionary<int, string> IntStringMap { get; set; }
      public Dictionary<int, Dictionary<string, string[]>[]> IntStringStringArrayMapArrayMap { get; set; }
      public Type Type { get; set; }
      public DateTime DateTime { get; set; }
      public float Float { get; set; }
      public double Double { get; set; }
      public ValueTuple<int, string> ValueTuple { get; set; }
      public HodgepodgeDto InnerHodgepodge { get; set; }
      public Vector3 ValueType;
      [N<N, N<N, (N, P)>>] public Dictionary<int, Dictionary<string, (Dog, Animal)>> MixedPolymorphicTest { get; set; }
      [P] public Animal Animal { get; set; }
   }

   [VoxType((int)BuiltInVoxTypeIds.Int32, RedirectToType = typeof(Int32), Flags = VoxTypeFlags.StubRaw | VoxTypeFlags.NonUpdatable)]
   public static partial class VoxGeneration_Int32 {
      public static partial void Stub_WriteRaw_Int32(VoxWriter writer, int value) => writer.InnerWriter.Write(value);
      public static partial int Stub_ReadRaw_Int32(VoxReader reader) => reader.InnerReader.ReadInt32();
   }

   [VoxType((int)BuiltInVoxTypeIds.String, RedirectToType = typeof(String), Flags = VoxTypeFlags.StubRaw | VoxTypeFlags.NonUpdatable)]
   public static partial class VoxGeneration_String {
      public static partial void Stub_WriteRaw_String(VoxWriter writer, string value) => writer.InnerWriter.Write(value);
      public static partial string Stub_ReadRaw_String(VoxReader reader) => reader.InnerReader.ReadString();
   }

   [VoxType((int)BuiltInVoxTypeIds.Guid, RedirectToType = typeof(Guid), Flags = VoxTypeFlags.StubRaw | VoxTypeFlags.NonUpdatable)]
   public static partial class VoxGeneration_Guid {
      public static partial void Stub_WriteRaw_Guid(VoxWriter writer, Guid value) => writer.InnerWriter.Write(value);
      public static partial Guid Stub_ReadRaw_Guid(VoxReader reader) => reader.InnerReader.ReadGuid();
   }

   public static class VoxReflectionCache_Helpers {
      public static byte[] ListLikeTypeIdBytes = ((int)BuiltInVoxTypeIds.ListLike).ToVariableIntBytes();
   }

   public static class VoxReflectionCache {
      public static void HintSerializer<T>(IVoxSerializer<T> inst) {
         VoxReflectionCache<T>.HintSerializer(inst);
      }
   }

   /// <summary>
   /// Reflection cache, primarily for looking up the serializer of a given type.
   /// Note: Types' serializers cannot rely upon ReflectionCache to provide their typeId/typeIdBytes,
   /// as such values are derived from the TypeSerializer instance (otherwise we'd have a circular
   /// dependency).
   /// </summary>
   /// <typeparam name="T"></typeparam>
   // ReSharper disable StaticMemberInGenericType
   public static class VoxReflectionCache<T> {
      private static byte[]? typeIdBytes;
      private static IVoxSerializer<T>? serializer;

      static VoxReflectionCache() {
         if (typeof(T).IsAssignableTo(typeof(IVoxCustomType<T>))) {
            var tInst = (IVoxCustomType<T>)(object)Activator.CreateInstance<T>()!;
            serializer = tInst.Serializer;
         }
      }

      /// <summary>
      /// Informs the reflection cache that the given serializer instance exists.
      /// This matters because serializer constructors are recursive; a serializer
      /// constructor of a type might request the serializer of field's type. To
      /// make these calls succeed, serializers should invoke <see cref="HintSerializer"/>
      /// as their first operation.
      /// </summary>
      /// <param name="inst"></param>
      public static void HintSerializer(IVoxSerializer<T> inst) => serializer ??= inst;

      // private static void EnsureInitialized_HintTIsListlikeWithElementType<TElement>() {
      //    var elementTypeIdBytes = VoxReflectionCache<TElement>.GetTypeIdBytes();
      //    var elementSerializer = VoxReflectionCache<TElement>.GetSerializer();
      //    var listLikeTypeIdBytes = VoxReflectionCache_Helpers.ListLikeTypeIdBytes;
      //    var res = new byte[listLikeTypeIdBytes.Length + elementTypeIdBytes.Length];
      //    listLikeTypeIdBytes.AsSpan().CopyTo(res);
      //    elementTypeIdBytes.AsSpan().CopyTo(res.AsSpan(listLikeTypeIdBytes.Length));
      //    typeIdBytes = res;
      //    serializer = (IVoxSerializer<T>)(object)ArraySerializer<TElement>.Instance;
      // }

      // public static byte[] GetTypeIdBytes() {
      //
      // }

      public static IVoxSerializer<T> GetSerializer() => serializer.AssertIsNotNull();
   }

   // public sealed class ArraySerializer0 : IVoxSerializer<Array> {
   //    public void WriteFull(VoxWriter writer, ref Array val) => throw new InvalidOperationException();
   //    public void WriteRaw(VoxWriter writer, ref Array val) => throw new InvalidOperationException();
   //    public Array ReadFull(VoxReader reader) => throw new InvalidOperationException();
   //    public Array ReadRaw(VoxReader reader) => throw new InvalidOperationException();
   //    public void ReadFullIntoRef(VoxReader reader, ref Array val) => throw new InvalidOperationException();
   //    public void ReadRawIntoRef(VoxReader reader, ref Array val) => throw new InvalidOperationException();
   // }

   // public sealed class ValueTupleSerializer_1<T> : IVoxSerializer<ValueTuple<T>> { }
   // public sealed class ValueTupleSerializer_2<T1, T2> : IVoxSerializer<ValueTuple<T1, T2>> {
   //    public int[] FullTypeId { get; } = Arrays.Concat(new[] { (int)BuiltInVoxTypeIds.ListLike }, VoxReflectionCache<T1>.GetSerializer().FullTypeId, VoxReflectionCache<T2>.GetSerializer().FullTypeId);
   //    public byte[] FullTypeIdBytes { get; } = Arrays.Concat(((int)BuiltInVoxTypeIds.ListLike).ToVariableIntBytes(), VoxReflectionCache<T1>.GetSerializer().FullTypeIdBytes, VoxReflectionCache<T2>.GetSerializer().FullTypeIdBytes);
   //    public bool IsUpdatable => false;
   //
   //    public void WriteFull(VoxWriter writer, ref (T1, T2) val) {
   //       throw new NotImplementedException();
   //    }
   //
   //    public void WriteRaw(VoxWriter writer, ref (T1, T2) val) {
   //       throw new NotImplementedException();
   //    }
   //
   //    public (T1, T2) ReadFull(VoxReader reader) {
   //       throw new NotImplementedException();
   //    }
   //
   //    public (T1, T2) ReadRaw(VoxReader reader) {
   //       throw new NotImplementedException();
   //    }
   //
   //    public void ReadFullIntoRef(VoxReader reader, ref (T1, T2) val) {
   //       throw new NotImplementedException();
   //    }
   //
   //    public void ReadRawIntoRef(VoxReader reader, ref (T1, T2) val) {
   //       throw new NotImplementedException();
   //    }
   // }

   // public abstract class ListLikeSerializerBase<T, TCollection, TBool_TIsPolymorphic> : IVoxSerializer<TCollection> {
   //    public static readonly IVoxSerializer<T> ElementSerializer = VoxReflectionCache<T>.GetSerializer();
   //    public static readonly bool ElementIsNonPolymorphic = TBool.IsTrue<TBool_TIsPolymorphic>();
   //
   //    public int[] FullTypeId { get; } = Arrays.Concat(new[] { (int)BuiltInVoxTypeIds.ListLike }, VoxReflectionCache<T>.GetSerializer().FullTypeId);
   //    public byte[] FullTypeIdBytes { get; } = Arrays.Concat(((int)BuiltInVoxTypeIds.ListLike).ToVariableIntBytes(), VoxReflectionCache<T>.GetSerializer().FullTypeIdBytes);
   //
   //    public abstract bool IsUpdatable { get; }
   //
   //    public void WriteTypeIdBytes(VoxWriter writer) => writer.WriteRawBytes(FullTypeIdBytes);
   //
   //    public void AssertReadTypeId(VoxReader reader) => reader.AssertReadRawBytes(FullTypeIdBytes);
   //
   //    public void WriteFull(VoxWriter writer, ref TCollection val) {
   //       WriteTypeIdBytes(writer);
   //       WriteRaw(writer, ref val);
   //    }
   //
   //    public abstract void WriteRaw(VoxWriter writer, ref TCollection val);
   //
   //    protected void WriteElement(VoxWriter writer, ref T item) {
   //       if (ElementIsNonPolymorphic) {
   //          ElementSerializer.WriteRaw(writer, ref item);
   //       } else {
   //          throw new NotImplementedException();
   //       }
   //    }
   //
   //    public TCollection ReadFull(VoxReader reader) {
   //       AssertReadTypeId(reader);
   //       return ReadRaw(reader);
   //    }
   //
   //    public abstract TCollection ReadRaw(VoxReader reader);
   //
   //    protected void ReadElement(VoxReader reader, ref T res) {
   //       if (ElementIsNonPolymorphic) {
   //          if (ElementSerializer.IsUpdatable) {
   //             ElementSerializer.ReadRawIntoRef(reader, ref res);
   //          } else {
   //             res = ElementSerializer.ReadRaw(reader);
   //          }
   //       } else {
   //          throw new NotImplementedException();
   //       }
   //    }
   //
   //    public void ReadFullIntoRef(VoxReader reader, ref TCollection val) {
   //       AssertReadTypeId(reader);
   //       ReadRawIntoRef(reader, ref val);
   //    }
   //
   //    public abstract void ReadRawIntoRef(VoxReader reader, ref TCollection val);
   // }
   //
   // /// <summary>Autogenerated</summary>
   // public sealed class ArraySerializer<T, TBool_TIsPolymorphic> : ListLikeSerializerBase<T, T[], TBool_TIsPolymorphic> {
   //    public static readonly ArraySerializer<T, TBool_TIsPolymorphic> Instance = new();
   //    
   //    public ArraySerializer() => VoxReflectionCache.HintSerializer(this);
   //
   //    public override bool IsUpdatable => false;
   //    
   //    public override void WriteRaw(VoxWriter writer, ref T[] self) {
   //       writer.InnerWriter.Write((int)self.Length);
   //       for (var i = 0; i < self.Length; i++) {
   //          WriteElement(writer, ref self[i]);
   //       }
   //    }
   //
   //    public override T[] ReadRaw(VoxReader reader) {
   //       var len = reader.InnerReader.ReadInt32();
   //       var res = new T[len];
   //       if (ElementIsNonPolymorphic) {
   //          for (var i = 0; i < len; i++) {
   //             ReadElement(reader, ref res[i]);
   //          }
   //       } else {
   //          throw new NotImplementedException();
   //       }
   //
   //       return res;
   //    }
   //
   //    public override void ReadRawIntoRef(VoxReader reader, ref T[] self) => throw new NotSupportedException();
   // }

   public class Animal {}
   public class Dog : Animal {}
   public class Cat : Animal {}
}