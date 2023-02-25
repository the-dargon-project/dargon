using System.Numerics;
using Dargon.Commons;
using Dargon.Commons.Collections;
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

   public class VoxSerializerContainer {
      private readonly Dictionary<Type, IVoxSerializer> typeToSerializer = new();
      private readonly object sync = new();

      private readonly VoxContext context;

      public VoxSerializerContainer(VoxContext context) {
         this.context = context;
      }

      public IVoxSerializer GetOrCreate(Type type) {
         lock (sync) {
            context.GetTrieNodeOfTypeOrThrow(type);
            Activator.CreateInstance(type);
         }
      }
   }

   public static class VoxTypeUtils {
      internal static bool VoxInternal_IsCompleteType(this Type t) => !t.IsGenericType || t.IsConstructedGenericType;

      internal static Type AssertIsCompleteType(this Type t) {
         Assert.IsTrue(t.VoxInternal_IsCompleteType());
         return t;
      }
   }

   public class VoxTypeTrieContainer {
      // these fields are written to by a single thread at init-time
      private readonly Dictionary<Type, VoxTypeContext> simpleTypeToTrieRootContext = new();
      private readonly Dictionary<int, VoxTypeContext> simpleTypeIdToTrieRootContext = new();

      // dynamically populated at runtime. Ultimately the tries are the source of truth.
      private readonly CopyOnAddDictionary<Type, VoxTypeTrieNode> completeTypeToNodeCache = new();

      public void ImportTypeContext(VoxTypeContext c) {
         simpleTypeToTrieRootContext.Add(c.SimpleType, c);
         simpleTypeIdToTrieRootContext.Add(c.SimpleTypeId, c);
         if (c.CompleteTypeOrNull is { } ct) {
            completeTypeToNodeCache.Add(ct, c);
         }
      }

      public VoxTypeContext GetTrieRootContextOfSimpleTypeId(int simpleTypeId) => simpleTypeIdToTrieRootContext[type];
      public VoxTypeContext GetTrieRootContextOfSimpleType(Type type) => simpleTypeToTrieRootContext[type];

      public VoxTypeTrieNode GetOrCreateTrieNodeOfCompleteType(Type type) {
         if (completeTypeToNodeCache.TryGetValue(type, out var existing)) return existing; // look ma, no locks!
         var res = GetOrCreateTrieNodeOfCompleteType_SlowPath(type);
         return completeTypeToNodeCache.GetOrAdd(type, res);
      }

      private VoxTypeTrieNode GetOrCreateTrieNodeOfCompleteType_SlowPath(Type type) {
         type.VoxInternal_IsCompleteType().AssertIsTrue();
         type.IsGenericType.AssertIsTrue();

         VoxTypeTrieNode current = simpleTypeToTrieRootContext[type.GetGenericTypeDefinition()];
         foreach (var gta in type.GenericTypeArguments) {
            current = GetOrCreateTrieNodeOfCompleteType_RecurseToTrieLeaf(gta, current);
         }
         return current;
      }

      private VoxTypeTrieNode GetOrCreateTrieNodeOfCompleteType_RecurseToTrieLeaf(Type t, VoxTypeTrieNode current) {
         // case 1: t is a simple type
         if (simpleTypeToTrieRootContext.TryGetValue(t, out var tCtx)) {
            return current.TypeIdToChildNode.GetOrAdd(
               tCtx.SimpleTypeId, 
               (current, t), 
               (stid, x) => new(stid, x.t, x.current));
         }

         // case 2: t is a complete generic type - visit its generic type definition, then args
         current = GetOrCreateTrieNodeOfCompleteType_RecurseToTrieLeaf(t.GetGenericTypeDefinition(), current);
         foreach (var gta in t.GenericTypeArguments) {
            current = GetOrCreateTrieNodeOfCompleteType_RecurseToTrieLeaf(gta, current);
         }
      }
   }

   public static class VoxContextFactory {
      public static VoxContext Create(VoxTypes voxTypes) {
         var trieContainer = new VoxTypeTrieContainer();

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
                  SerializerType = tSerializer,
                  Internal_Type_GenericArguments_Cache = type.IsGenericType ? type.GenericTypeArguments : Type.EmptyTypes,
                  SerializerInstanceOrNull = null,
               };
               typeContext.LateInitialize(type.If(type.VoxInternal_IsCompleteType()));
               trieContainer.ImportTypeContext(typeContext);
            }

            foreach (var t in vt.DependencyVoxTypes) {
               Visit((VoxTypes)Activator.CreateInstance(t)!);
            }

         }

         Visit(voxTypes);

         return new VoxContext(voxTypes, simpleTypeIdToContext, typeToContext);
      }
   }

   public class VoxContext {
      private readonly VoxTypes voxTypes;
      private readonly VoxTypeTrieContainer trieContainer;

      internal VoxContext(VoxTypes voxTypes, VoxTypeTrieContainer trieContainer) {
         this.voxTypes = voxTypes;
         this.trieContainer = trieContainer;
      }

      public VoxTypes VoxTypes => voxTypes;
      public VoxTypeContext GetContextOfTypeIdOrThrow(int typeId) => typeIdToContext[typeId];
      public VoxWriter CreateWriter(Stream s, bool leaveOpen = true) => new(s, leaveOpen, polymorphicSerializationOperationsAdapter);
      public VoxReader CreateReader(Stream s, bool leaveOpen = true) => new(s, leaveOpen, polymorphicSerializationOperationsAdapter);

      public static VoxContext Create(VoxTypes voxTypes) => VoxContextFactory.Create(voxTypes);

      private class PolymorphicSerializationOperationsAdapter : IPolymorphicSerializationOperations {
         private readonly VoxTypeTrieContainer trieContainer;

         public PolymorphicSerializationOperationsAdapter(VoxTypeTrieContainer trieContainer) {
            this.trieContainer = trieContainer;
         }

         public Type ReadFullType(VoxReader reader) => ReadFullTypeInternal(reader).CompleteTypeOrNull.AssertIsNotNull();


         private VoxTypeTrieNode ReadFullTypeInternal(VoxReader reader) {
            var rootTid = reader.ReadSimpleTypeId();
            var typeContext = trieContainer.GetTrieRootContextOfSimpleTypeId(rootTid);

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
                  // type is not yet imported into the trie. backtrack, then do a full type read.
                  var s = new Stack<VoxTypeTrieNode>(child.Dive(x => x.ParentOrNull));
                  s.Peek().AssertNotEquals(child);

                  // Complete the type-read, then import into the trie.
                  var (type, readTids) = CompleteTypeReadWithoutTrie(reader, s);

               }
            }
         }

         private (Type, List<int> additionalTypeIdReads) CompleteTypeReadWithoutTrie(VoxReader reader, Stack<VoxTypeTrieNode> s) {
            var additionalTypeIdReads = new List<int>();
            var root = s.Peek();
            Type RecurseToReadType() {
               VoxTypeContext typeContext;
               if (s.Count > 0) {
                  typeContext = context.GetContextOfTypeIdOrThrow(s.Pop().SimpleTypeId);
               } else {
                  var typeId = reader.ReadSimpleTypeId();
                  typeContext = context.GetContextOfTypeIdOrThrow(typeId);
                  additionalTypeIdReads.Add(typeId);
               }

               var type = typeContext.Type;
               if (typeContext.Internal_Type_GenericArguments_Cache.Length == 0) {
                  return type;
               }

               var numTypeArguments = typeContext.Internal_Type_GenericArguments_Cache.Length;
               var typeArguments = new Type[numTypeArguments];
               for (var i = 0; i < numTypeArguments; i++) {
                  typeArguments[i] = RecurseToReadType();
               }

               return type.MakeGenericType(typeArguments);
            }

            return (RecurseToReadType(), additionalTypeIdReads);
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
      public required Type SerializerType;
      public required Type[] Internal_Type_GenericArguments_Cache;
      public VoxTypeTrieNode RootNode => this;
      
      public VoxTypeContext(int simpleTypeId, Type simpleType) : base(simpleTypeId, simpleType, null) { }
   }

   public class VoxTypeTrieNode {
      public VoxTypeTrieNode(int simpleTypeId, Type simpleType, VoxTypeTrieNode? parentOrNull) {
         SimpleTypeId = simpleTypeId;
         SimpleType = simpleType;
         ParentOrNull = parentOrNull;
      }

      public int SimpleTypeId { get; }
      public Type SimpleType { get; }
      public VoxTypeTrieNode? ParentOrNull { get; }
      public Type? CompleteTypeOrNull { get; private set; } // Late-initialized immediately after construction

      internal void LateInitialize(Type? completeTypeOrNull) {
         CompleteTypeOrNull = completeTypeOrNull;
      }

      public CopyOnAddDictionary<int, VoxTypeTrieNode> TypeIdToChildNode { get; } = new();
      public IVoxSerializer? SerializerInstanceOrNull { get; set; }
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

   public class RuntimePolymorphicArraySerializer<T> : IVoxSerializer<T[]> {
      public int SimpleTypeId { get; }
      public int[] FullTypeId { get; }
      public byte[] FullTypeIdBytes { get; }
      public bool IsUpdatable { get; }

      public void WriteFullObject(VoxWriter writer, object val) {
         throw new NotImplementedException();
      }

      public object ReadRawAsObject(VoxReader reader) {
         throw new NotImplementedException();
      }

      public void WriteFull(VoxWriter writer, ref T[] val) {
         throw new NotImplementedException();
      }

      public void WriteRaw(VoxWriter writer, ref T[] val) {
         throw new NotImplementedException();
      }

      public T[] ReadFull(VoxReader reader) {
         throw new NotImplementedException();
      }

      public T[] ReadRaw(VoxReader reader) {
         throw new NotImplementedException();
      }

      public void ReadFullIntoRef(VoxReader reader, ref T[] val) {
         throw new NotImplementedException();
      }

      public void ReadRawIntoRef(VoxReader reader, ref T[] val) {
         throw new NotImplementedException();
      }
   }

   public class Animal {}
   public class Dog : Animal {}
   public class Cat : Animal {}
}