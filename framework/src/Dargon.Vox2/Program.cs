using System.Numerics;
using System.Runtime.CompilerServices;
using Dargon.Commons;
using Dargon.Commons.Collections;
using Dargon.Commons.Templating;
using Dargon.Commons.Utilities;

namespace Dargon.Vox2 {
   public class Program {
      static void Main(string[] args) {
         Console.WriteLine("Hello, World!");

         var vox = VoxContext.Create(new TestVoxTypes());

         using var ms = new MemoryStream();
         using var vw = vox.CreateWriter(ms);
         vw.WritePolymorphic(new SimpleTestType { i = 10, s = "Hello, World!"});
         ms.Position = 0;
         using var vr = vox.CreateReader(ms);
         var rt = (SimpleTestType)vr.ReadPolymorphic();
         rt.i.AssertEquals(10);
         rt.s.AssertEquals("Hello, World!");

         // vw.WriteFull(new HodgepodgeMin());
      }
   }

   public class VoxContext {
      private readonly VoxTypes voxTypes;
      private readonly Dictionary<int, VoxTypeContext> typeIdToContext;
      private readonly Dictionary<Type, VoxTypeContext> typeToContext;
      private readonly CopyOnAddDictionary<Type, VoxTypeTrieNode> typeToNode;
      private readonly PolymorphicSerializationOperationsAdapter polymorphicSerializationOperationsAdapter;

      private VoxContext(VoxTypes voxTypes, Dictionary<int, VoxTypeContext> typeIdToContext, Dictionary<Type, VoxTypeContext> typeToContext) {
         this.voxTypes = voxTypes;
         this.typeIdToContext = typeIdToContext;
         this.typeToContext = typeToContext;
         this.typeToNode = new(typeToContext.Map(x => x.RootNode));
         this.polymorphicSerializationOperationsAdapter = new(this);
      }

      public VoxTypes VoxTypes => voxTypes;

      public VoxTypeContext GetContextOfTypeIdOrThrow(int typeId) => typeIdToContext[typeId];

      public VoxTypeTrieNode GetTrieNodeOfTypeOrThrow(Type type) {
         if (typeToNode.TryGetValue(type, out var cachedResult)) {
            return cachedResult;
         }

         var s = new Stack<Type>();
         s.Push(type);

         VoxTypeTrieNode DecorateIfTerminal(int stid, VoxTypeTrieNode node) {
            if (s.Count == 0) {
               node.CompleteTypeOrNull = type;
               var serializerType = typeToContext[type.GetGenericTypeDefinition()].SerializerType.MakeGenericType(type.GenericTypeArguments);
               node.SerializerInstanceOrNull = (IVoxSerializer)Activator.CreateInstance(serializerType).AssertIsNotNull();
            }
            return node;
         }

         VoxTypeTrieNode? current = null;
         while (s.Count > 0) {
            var t = s.Pop();
            
            VoxTypeContext? tCtx;
            if (typeToContext.TryGetValue(t, out tCtx)) {
               var next = new VoxTypeTrieNode(tCtx.SimpleTypeId, t, current);
               current = current!.TypeIdToChildNode.GetOrAdd(tCtx.SimpleTypeId, next, DecorateIfTerminal);
               continue;
            }

            var gtd = t.GetGenericTypeDefinition();
            var gtdCtx = typeToContext[gtd];
            if (current == null) {
               current = typeToNode[gtd];
            } else {
               var next = new VoxTypeTrieNode(gtdCtx.SimpleTypeId, gtd, current);
               current = current.TypeIdToChildNode.GetOrAdd(gtdCtx.SimpleTypeId, next); // can't be terminal.
            }

            var gtas = t.GenericTypeArguments;
            for (var i = gtas.Length - 1; i >= 0; i--) {
               s.Push(gtas[i]);
            }
         }

         current!.CompleteTypeOrNull.AssertIsNotNull();
         current.SerializerInstanceOrNull.AssertIsNotNull();
         return current;
      }
      
      public VoxWriter CreateWriter(Stream s, bool leaveOpen = true) => new(s, leaveOpen, polymorphicSerializationOperationsAdapter);
      public VoxReader CreateReader(Stream s, bool leaveOpen = true) => new(s, leaveOpen, polymorphicSerializationOperationsAdapter);

      public static VoxContext Create(VoxTypes voxTypes) {
         var simpleTypeIdToContext = new Dictionary<int, VoxTypeContext>();
         var typeToContext = new Dictionary<Type, VoxTypeContext>();

         void Visit(VoxTypes vt) {
            var typeToSerializer = new Dictionary<Type, Type>(vt.TypeToCustomSerializers);
            foreach (var t in vt.AutoserializedTypes) {
               typeToSerializer.Add(
                  t, ((IVoxCustomType)Activator.CreateInstance(t)!).Serializer.GetType());
            }

            foreach (var (type, tSerializer) in typeToSerializer) {
               var typeAttr1 = type.GetAttributeOrNull<VoxTypeAttribute>();
               var typeAttr2 = tSerializer.GetAttributeOrNull<VoxTypeAttribute>();
               
               if (typeAttr1 == null && typeAttr2 == null) {
                  throw new ArgumentException($"Need {nameof(VoxTypeAttribute)} for either type {type} or its serializer {tSerializer}.");
               } else if (typeAttr1 != null && typeAttr2 != null && typeAttr1.Id != typeAttr2.Id) {
                  throw new ArgumentException($"Different typeIds specified for type ${type} ({typeAttr1.Id}) vs its serializer {tSerializer} ({typeAttr2.Id})");
               }

               var mergedTypeAttrs = new VoxTypeAttribute(typeAttr1?.Id ?? typeAttr2.Id) {
                  Flags = (typeAttr1?.Flags ?? 0) | (typeAttr2?.Flags ?? 0),
                  RedirectToType = typeAttr1?.RedirectToType ?? typeAttr2?.RedirectToType,
               };

               if (type.IsGenericType && !type.IsGenericTypeDefinition) {
                  // specialization
                  mergedTypeAttrs.Flags.AssertHasFlags(VoxTypeFlags.Specialization);
                  type.IsConstructedGenericType.AssertIsTrue();
               } else {
                  mergedTypeAttrs.Flags.AssertHasUnsetFlags(VoxTypeFlags.Specialization);
                  if (type.IsGenericType) {
                     type.IsConstructedGenericType.AssertEquals(!type.IsGenericTypeDefinition);
                  }
               }

               var simpleTypeId = mergedTypeAttrs.Id;

               var typeContext = new VoxTypeContext(simpleTypeId, type) {
                  Type = type,
                  SerializerType = tSerializer,
                  Internal_Type_GenericArguments_Cache = type.IsGenericType ? type.GenericTypeArguments : Type.EmptyTypes,
                  CompleteTypeOrNull = type.IsGenericType ? null : type,
                  SerializerInstanceOrNull = type.IsConstructedGenericType || !type.IsGenericType ? (IVoxSerializer)Activator.CreateInstance(tSerializer)! : null,
               };
               
               simpleTypeIdToContext.Add(simpleTypeId, typeContext);
               typeToContext.Add(type, typeContext);
            }

            foreach (var t in vt.DependencyVoxTypes) {
               Visit((VoxTypes)Activator.CreateInstance(t)!);
            }

         }

         Visit(voxTypes);

         return new VoxContext(voxTypes, simpleTypeIdToContext, typeToContext);
      }

      private class PolymorphicSerializationOperationsAdapter : IPolymorphicSerializationOperations {
         private readonly VoxContext context;

         public PolymorphicSerializationOperationsAdapter(VoxContext context) {
            this.context = context;
         }

         public Type ReadFullType(VoxReader reader) => ReadFullTypeInternal(reader).CompleteTypeOrNull.AssertIsNotNull();


         private VoxTypeTrieNode ReadFullTypeInternal(VoxReader reader) {
            var rootTid = reader.ReadSimpleTypeId();
            var typeContext = context.GetContextOfTypeIdOrThrow(rootTid);

            // traverse the trie as far as possible, terminating if we hit a terminal node (one which has a complete type)
            // or can no longer go deeper.
            var currentNode = typeContext.RootNode;
            while (true) {
               if (currentNode.CompleteTypeOrNull != null) {
                  return currentNode;
               }

               var tid = reader.ReadSimpleTypeId();
               if (currentNode.TypeIdToChildNode.TryGetElseAdd(tid, (currentNode, context), 
                      static (tid, x) => new VoxTypeTrieNode(tid, x.context.GetContextOfTypeIdOrThrow(tid).Type, x.currentNode), 
                      out var child)) {
                  currentNode = child;
               } else {
                  // added a child, meaning this type doesn't exist in the trie.
                  return ReadFullTypeInternal_FirstTime(reader, child);
               }
            }
         }

         private VoxTypeTrieNode ReadFullTypeInternal_FirstTime(VoxReader reader, VoxTypeTrieNode n) {
            // backtrack
            var s = new Stack<VoxTypeTrieNode>();
            for (var cur = n; cur != null; cur = cur.ParentOrNull) {
               s.Push(cur);
            }

            var leaf = n;
            var root = s.Peek();

            // consume
            Type ReadFullType_FirstTime_Helper() {
               VoxTypeContext typeContext;
               if (s.Count > 0) {
                  typeContext = context.GetContextOfTypeIdOrThrow(s.Pop().SimpleTypeId);
               } else {
                  var typeId = reader.ReadSimpleTypeId();
                  typeContext = context.GetContextOfTypeIdOrThrow(typeId);
                  leaf = leaf.TypeIdToChildNode.GetOrAdd(
                     typeId, 
                     (typeContext.Type, leaf), 
                     (tid, x) => new VoxTypeTrieNode(tid, x.Type, x.leaf));
               }

               var type = typeContext.Type;
               if (typeContext.Internal_Type_GenericArguments_Cache.Length == 0) {
                  return type;
               }

               var numTypeArguments = typeContext.Internal_Type_GenericArguments_Cache.Length;
               var typeArguments = new Type[numTypeArguments];
               for (var i = 0; i < numTypeArguments; i++) {
                  typeArguments[i] = ReadFullType_FirstTime_Helper();
               }

               return type.MakeGenericType(typeArguments);
            }

             ;
            var completeType = leaf.CompleteTypeOrNull = ReadFullType_FirstTime_Helper();
            var serializerType = context.GetContextOfTypeIdOrThrow(root.SimpleTypeId).SerializerType.MakeGenericType(completeType.GenericTypeArguments);
            leaf.SerializerInstanceOrNull = (IVoxSerializer)Activator.CreateInstance(serializerType).AssertIsNotNull();
            return leaf;
         }

         public T ReadPolymorphicFull<T>(VoxReader reader) {
            var tn = ReadFullTypeInternal(reader);
            var seri = tn.SerializerInstanceOrNull.AssertIsNotNull();
            return (T)seri.ReadRawAsObject(reader);
         }

         public void WriteFullType(VoxWriter writer, Type type) {
            var tn = context.GetTrieNodeOfTypeOrThrow(type);
            writer.WriteTypeIdBytes(tn.SerializerInstanceOrNull!.FullTypeIdBytes);
         }

         public void WritePolymorphicFull<T>(VoxWriter writer, ref T x) {
            var tn = context.GetTrieNodeOfTypeOrThrow(typeof(T));
            tn.SerializerInstanceOrNull!.WriteFullObject(writer, x);
         }
      }
   }

   /// <summary>
   /// As a micro-optimization, inherits TrieNode so the root
   /// node's access (in the nongeneric case) has 1 less indirection.
   /// </summary>
   public class VoxTypeContext : VoxTypeTrieNode {
      public required Type Type;
      public required Type SerializerType;
      public required Type[] Internal_Type_GenericArguments_Cache;
      public VoxTypeTrieNode RootNode => this;
      
      public VoxTypeContext(int simpleTypeId, Type simpleType) : base(simpleTypeId, simpleType, null) { }
   }

   public class VoxTypeTrieNode {
      public VoxTypeTrieNode(int simpleTypeId, Type simpleType, VoxTypeTrieNode? parent = null) {
         SimpleTypeId = simpleTypeId;
         SimpleType = simpleType;
         ParentOrNull = parent;
      }

      public int SimpleTypeId { get; }
      public Type SimpleType { get; }

      public readonly CopyOnAddDictionary<int, VoxTypeTrieNode> TypeIdToChildNode = new();
      public readonly VoxTypeTrieNode? ParentOrNull;
      public IVoxSerializer? SerializerInstanceOrNull;
      public Type? CompleteTypeOrNull;
   }

   /// <summary>
   /// Note: Do not register:
   /// * Arrays, Maps
   /// * Tuples
   /// These are handled specially by vox; when serialized as a field, a built-in type is used.
   /// When serialized directly, a box wraps them.
   /// </summary>
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
   public class LAttribute<T> : VoxInternalBaseAttribute { }
   public class DAttribute<TKey, TValue> : VoxInternalBaseAttribute { }


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
      [D<N, D<P, N[]>[][]>] public Dictionary<int, Dictionary<object, int[]>[][]> DictOfIntToArrayOfArrayOfDictOfStringToIntArray { get; set; }
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