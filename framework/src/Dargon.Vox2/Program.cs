using System.Numerics;
using Dargon.Commons;
using Dargon.Commons.Collections;
using Dargon.Commons.Exceptions;
using Dargon.Commons.Utilities;

namespace Dargon.Vox2 {
   public class Program {
      static void Main(string[] args) {
         Console.WriteLine("Hello, World!");

         var hodgepodgeOriginal = new HodgepodgeMin {
            Int32 = 10,
            String = "Hello, World!",
            Guid = new Guid(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1),
            IntArray = new int[] { 1, 2, 3 },
            IntList = new() { 21, 22, 23 },
            DictOfIntToArrayOfArrayOfDictOfStringToIntArray = new() {
               [123] = new Dictionary<object, int[]>[][] {
                  new Dictionary<object, int[]>[] {
                     new Dictionary<object, int[]> {
                        ["Key1"] = new int[] { 1, 2, 3, 4, },
                        ["Key2"] = new int[] { 11, 22, 33, 44, },
                     },
                     new Dictionary<object, int[]> {
                        ["Key3"] = new int[] { 111, 222, 333, 444, },
                     },
                  },
                  new Dictionary<object, int[]>[0],
               },
               [234] = new Dictionary<object, int[]>[0][],
            },
            PolymorphicString = "abc",
            PolymorphicIntArray = new int[] { 123, 456, 789 },
            PolymorphicIntList = new List<int> { 31, 32, 32 },
            PolymorphicIntToIntArrayDict = new Dictionary<int, int[]> {
               [420] = new[] { 81423, 17 },
            },
         };

         var voxForWriter = VoxContext.Create(new TestVoxTypes());
         using var ms = new MemoryStream();
         using var vw = voxForWriter.CreateWriter(ms);
         vw.WritePolymorphic(hodgepodgeOriginal);
         var writeLen = ms.Position;
         ms.Position = 0;

         var voxForReader = VoxContext.Create(new TestVoxTypes());
         using var vr = voxForReader.CreateReader(ms);
         var rt = (HodgepodgeMin)vr.ReadPolymorphic();
         hodgepodgeOriginal.Int32.AssertEquals(rt.Int32);
         hodgepodgeOriginal.String.AssertEquals(rt.String);
         hodgepodgeOriginal.Guid.AssertEquals(rt.Guid);
         hodgepodgeOriginal.IntArray.Length.AssertEquals(rt.IntArray.Length);
         foreach (var (i, x) in hodgepodgeOriginal.IntArray.Enumerate()) {
            rt.IntArray[i].AssertEquals(x);
         }

         hodgepodgeOriginal.IntList.Count.AssertEquals(rt.IntList.Count);
         foreach (var (i, x) in hodgepodgeOriginal.IntList.Enumerate()) {
            rt.IntList[i].AssertEquals(x);
         }

         var dictOrig = hodgepodgeOriginal.DictOfIntToArrayOfArrayOfDictOfStringToIntArray;
         var dictRead = rt.DictOfIntToArrayOfArrayOfDictOfStringToIntArray;
         var codeOrig = dictOrig.ToCodegenDump();
         var codeRead = dictRead.ToCodegenDump();
         codeOrig.AssertEquals(codeRead);

         hodgepodgeOriginal.PolymorphicString.AssertEquals(rt.PolymorphicString);
         
         var hoPIA = (int[])hodgepodgeOriginal.PolymorphicIntArray;
         var rtPIA = (int[])rt.PolymorphicIntArray;
         hoPIA.Length.AssertEquals(rtPIA.Length);
         foreach (var (i, x) in hoPIA.Enumerate()) {
            rtPIA[i].AssertEquals(x);
         }

         var hoPIL = (List<int>)hodgepodgeOriginal.PolymorphicIntList;
         var rtPIL = (List<int>)rt.PolymorphicIntList;
         hoPIA.Length.AssertEquals(rtPIL.Count);
         foreach (var (i, x) in hoPIL.Enumerate()) {
            rtPIL[i].AssertEquals(x);
         }

         var hoPIIAD = (Dictionary<int, int[]>)hodgepodgeOriginal.PolymorphicIntToIntArrayDict;
         var rtPIIAD = (Dictionary<int, int[]>)rt.PolymorphicIntToIntArrayDict;
         hoPIIAD.Count.AssertEquals(rtPIIAD.Count);
         foreach (var (k, v1) in hoPIIAD) {
            var v2 = rtPIIAD[k];
            v1.Length.AssertEquals(v2.Length);
            foreach (var (i, x) in v1.Enumerate()) {
               v2[i].AssertEquals(x);
            }
         }

         ms.Position.AssertEquals(writeLen);
         // rt.i.AssertEquals(10);
         // rt.s.AssertEquals("Hello, World!");

         // vw.WriteFull(new HodgepodgeMin());
      }
   }

   public class VoxSerializerContainer {
      private readonly VoxContext context;
      private readonly VoxTypeTrieContainer trieContainer;

      public VoxSerializerContainer(VoxContext context, VoxTypeTrieContainer trieContainer) {
         this.context = context;
         this.trieContainer = trieContainer;
      }

      public IVoxSerializer GetSerializerForType(Type t) {
         var trieNode = trieContainer.GetOrCreateTrieNodeOfCompleteType(t);
         return GetSerializerForTrieNode(trieNode);
      }

      public IVoxSerializer<T> GetSerializerForType<T>() {
         return (IVoxSerializer<T>)GetSerializerForType(typeof(T));
      }

      public IVoxSerializer GetSerializerForTrieNode(VoxTypeTrieNode trieNode) {
         var res = Interlocked2.Read(ref trieNode.SerializerCacheOrNull);
         if (res != null) return res;

         var vtc = trieNode.RootVoxTypeContext;
         var serializerType = vtc.SerializerType;
         if (serializerType.IsGenericTypeDefinition) {
            var completeType = trieNode.CompleteTypeOrNull.AssertIsNotNull();
            var args = trieNode.CompleteTypeInfoArgs;
            serializerType = serializerType.MakeGenericType(args);
         }

         var serializer = InstantiateSerializerForType(serializerType);
         return Interlocked2.AssignIfNull(ref trieNode.SerializerCacheOrNull, serializer);
      }

      /// <summary>
      /// When activating a serializer, it's possible that the serializer recursively
      /// requests another serializer from the container.
      /// </summary>
      /// <param name="tType">Type we're serializing</param>
      /// <param name="serializerType">The serializer type</param>
      public IVoxSerializer InstantiateSerializerForType(Type serializerType) {
         var ctors = serializerType.GetConstructors();
         foreach (var ctor in ctors) {
            var parameters = ctor.GetParameters();
            var args = new object[parameters.Length];
            for (var i = 0; i < args.Length; i++) {
               if (parameters[i].ParameterType == typeof(VoxContext)) {
                  args[i] = context;
               } else if (parameters[i].ParameterType == typeof(VoxTypeTrieContainer)) {
                  args[i] = trieContainer;
               } else if (parameters[i].ParameterType == typeof(VoxSerializerContainer)) {
                  args[i] = this;
               } else {
                  throw new NotImplementedException($"Unsure how to inject {parameters[i]}");
               }
            }

            return (IVoxSerializer)ctor.Invoke(args);
         }

         throw new InvalidOperationException($"Failed to construct {serializerType.FullName}; no found ctor?");
      }
   }

   public static class VoxTypeUtils {
      static VoxTypeUtils() {
         typeof(int).AssertIsCompleteType();
         typeof(string).AssertIsCompleteType();
         typeof(P<N>).AssertIsCompleteType();
         typeof(int[]).AssertIsCompleteType();
      }

      internal static bool VoxInternal_IsCompleteType(this Type t) => !t.IsGenericType || t.IsConstructedGenericType;

      internal static Type AssertIsCompleteType(this Type t) {
         Assert.IsTrue(t.VoxInternal_IsCompleteType());
         return t;
      }

      public static (Type tRoot, Type[] tArgs) UnpackTypeToVoxRootTypeAndArgs(Type t) {
         if (t.IsArray) {
            return (VoxTypePlaceholders.RuntimePolymorphicArray1Gtd, new[] { t.GetElementType().AssertIsNotNull() });
         } else if (t.IsGenericType) {
            return (t.GetGenericTypeDefinition(), t.GenericTypeArguments);
         } else {
            return (t, Type.EmptyTypes);
         }
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
            completeTypeToNodeCache.AddOrThrow(ct, c);
         }
      }

      public VoxTypeContext GetTrieRootContextOfSimpleTypeId(int simpleTypeId) => simpleTypeIdToTrieRootContext[simpleTypeId];
      public VoxTypeContext GetTrieRootContextOfSimpleType(Type type) => simpleTypeToTrieRootContext[type];

      public VoxTypeTrieNode GetOrCreateTrieNodeOfCompleteType(Type type) {
         if (completeTypeToNodeCache.TryGetValue(type, out var existing)) {
            existing.WaitForLateInitialize();
            return existing; // look ma, no locks!
         }

         var res = GetOrCreateTrieNodeOfCompleteType_SlowPath(type);
         return completeTypeToNodeCache.GetOrAdd(type, res);
      }

      private VoxTypeTrieNode GetOrCreateTrieNodeOfCompleteType_SlowPath(Type type) {
         type.VoxInternal_IsCompleteType().AssertIsTrue();

         var (tRoot, tArgs) = VoxTypeUtils.UnpackTypeToVoxRootTypeAndArgs(type);
         var rootContext = GetTrieRootContextOfSimpleType(tRoot);
         var current = rootContext.RootNode;
         foreach (var tArg in tArgs) {
            current = GetOrCreateTrieNodeOfCompleteType_RecurseToTrieLeaf(tArg, current);
         }

         current.LateInitializeTerminalNode(type, tRoot, tArgs);
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
         var (tGtd, tArgs) = VoxTypeUtils.UnpackTypeToVoxRootTypeAndArgs(t);
         current = GetOrCreateTrieNodeOfCompleteType_RecurseToTrieLeaf(tGtd, current);
         foreach (var tArg in tArgs) {
            current = GetOrCreateTrieNodeOfCompleteType_RecurseToTrieLeaf(tArg, current);
         }
         return current;
      }
   }

   public static class VoxContextFactory {
      public static VoxContext Create(VoxTypes voxTypes) {
         var res = new VoxContext { VoxTypes = voxTypes };
         var trieContainer = res.TrieContainer = new VoxTypeTrieContainer();
         var serializerContainer = res.SerializerContainer = new VoxSerializerContainer(res, trieContainer);
         var polymorphicSerializer = new PolymorphicSerializerImpl(trieContainer, serializerContainer);
         res.PolymorphicSerializer = polymorphicSerializer;

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
                  Internal_Type_GenericArguments_Cache = type.IsGenericTypeDefinition ? type.GetGenericArguments() : Type.EmptyTypes,
               };
               if (type.VoxInternal_IsCompleteType()) {
                  // var serializer = serializerActivator.InstantiateSerializer(type, typeContext.SerializerType);
                  var vtuInfo = VoxTypeUtils.UnpackTypeToVoxRootTypeAndArgs(type);
                  typeContext.LateInitializeTerminalNode(type, vtuInfo.tRoot, vtuInfo.tArgs);
               }

               trieContainer.ImportTypeContext(typeContext);
            }

            foreach (var t in vt.DependencyVoxTypes) {
               Visit((VoxTypes)Activator.CreateInstance(t)!);
            }

         }

         Visit(voxTypes);
         return res;
      }
   }

   public sealed class PolymorphicSerializerImpl : IPolymorphicSerializer {
      private readonly VoxTypeTrieContainer trieContainer;
      private readonly VoxSerializerContainer serializerContainer;

      public PolymorphicSerializerImpl(VoxTypeTrieContainer trieContainer, VoxSerializerContainer serializerContainer) {
         this.trieContainer = trieContainer;
         this.serializerContainer = serializerContainer;
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
            if (currentNode.TypeIdToChildNode.TryGetElseAdd(tid, (currentNode, trieContainer),
                   static (tid, x) => new VoxTypeTrieNode(tid, x.trieContainer.GetTrieRootContextOfSimpleTypeId(tid).SimpleType, x.currentNode),
                   out var child)) {
               currentNode = child;
            } else {
               // type is not yet imported into the trie. backtrack, then do a full type read.
               var s = new Stack<VoxTypeTrieNode>(child.Dive(x => x.ParentOrNull));
               s.Peek().AssertNotEquals(child);

               // Complete the type-read, then import into the trie.
               var (type, finalNode) = CompleteTypeReadWithoutTrie(reader, s);
               var vtuInfo = VoxTypeUtils.UnpackTypeToVoxRootTypeAndArgs(type);
               finalNode.LateInitializeTerminalNode(type, vtuInfo.tRoot, vtuInfo.tArgs);

               var serializer = finalNode.SerializerCacheOrNull;
               if (serializer == null) {
                  serializer = serializerContainer.GetSerializerForTrieNode(finalNode);
                  finalNode.SerializerCacheOrNull = serializer;
               }

               return finalNode;
            }
         }
      }

      private (Type, VoxTypeTrieNode currentNode) CompleteTypeReadWithoutTrie(VoxReader reader, Stack<VoxTypeTrieNode> s) {
         var additionalTypeIdReads = new List<int>();
         var currentNode = s.Peek();
         Type RecurseToReadType() {
            VoxTypeContext typeContext;
            if (s.Count > 0) {
               currentNode = s.Pop();
               typeContext = trieContainer.GetTrieRootContextOfSimpleTypeId(currentNode.SimpleTypeId);
            } else {
               var typeId = reader.ReadSimpleTypeId();
               typeContext = trieContainer.GetTrieRootContextOfSimpleTypeId(typeId);
               additionalTypeIdReads.Add(typeId);

               currentNode = currentNode.TypeIdToChildNode.GetOrAdd(
                  typeId,
                  (typeContext, currentNode),
                  static (stid, x) => new VoxTypeTrieNode(stid, x.typeContext.SimpleType, x.currentNode));
            }

            var type = typeContext.SimpleType;
            if (typeContext.Internal_Type_GenericArguments_Cache.Length == 0) {
               return type;
            }

            var numTypeArguments = typeContext.Internal_Type_GenericArguments_Cache.Length;
            var typeArguments = new Type[numTypeArguments];
            for (var i = 0; i < numTypeArguments; i++) {
               typeArguments[i] = RecurseToReadType();
            }

            if (type == VoxTypePlaceholders.RuntimePolymorphicArray1Gtd) {
               return typeArguments.FirstAndOnly().MakeArrayType();
            }

            return type.MakeGenericType(typeArguments);
         }

         return (RecurseToReadType(), currentNode);
      }

      public T ReadPolymorphicFull<T>(VoxReader reader) {
         var tn = ReadFullTypeInternal(reader);
         var seri = serializerContainer.GetSerializerForTrieNode(tn);
         return (T)seri.ReadRawObject(reader);
      }

      public void WriteFullType(VoxWriter writer, Type type) {
         var tn = trieContainer.GetOrCreateTrieNodeOfCompleteType(type);
         var seri = serializerContainer.GetSerializerForTrieNode(tn);
         writer.WriteTypeIdBytes(seri.FullTypeIdBytes);
      }

      public void WritePolymorphicFull<T>(VoxWriter writer, ref T x) {
         if (x == null) {
            throw new NotYetImplementedException();
         }

         var tn = trieContainer.GetOrCreateTrieNodeOfCompleteType(x.GetType());
         var seri = serializerContainer.GetSerializerForTrieNode(tn);
         writer.WriteTypeIdBytes(seri.FullTypeIdBytes);
         seri.WriteRawObject(writer, x);
      }
   }

   public class VoxContext {
      public VoxTypes VoxTypes { get; set; }
      public VoxTypeTrieContainer TrieContainer { get; set; }
      public IPolymorphicSerializer PolymorphicSerializer { get; set; }
      public VoxSerializerContainer SerializerContainer { get; set; }

      public VoxWriter CreateWriter(Stream s, bool leaveOpen = true) => new(s, PolymorphicSerializer, leaveOpen);
      public VoxReader CreateReader(Stream s, bool leaveOpen = true) => new(s, PolymorphicSerializer, leaveOpen);

      public static VoxContext Create(VoxTypes voxTypes) => VoxContextFactory.Create(voxTypes);
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

   /// <summary>
   /// Trie walked at deserialization to deserialize a type.
   /// Cannot be circularly dependent, as it is a trie node, so from any context it is
   /// safe to recursively attempt to get a type's TrieNode.
   ///
   /// Tagged with a serializer instance, to accelerate lookups.
   /// However, the serializer instances are backed by the
   /// <seealso cref="VoxSerializerContainer"/>
   /// </summary>
   public class VoxTypeTrieNode {
      public VoxTypeTrieNode(int simpleTypeId, Type simpleType, VoxTypeTrieNode? parentOrNull) {
         SimpleTypeId = simpleTypeId;
         SimpleType = simpleType;
         ParentOrNull = parentOrNull;
      }

      public int SimpleTypeId { get; }
      public Type SimpleType { get; }
      public VoxTypeTrieNode? ParentOrNull { get; }

      private VoxTypeContext? rootNodeCache = null;
      public VoxTypeContext RootVoxTypeContext => rootNodeCache ??= (ParentOrNull?.RootVoxTypeContext ?? (VoxTypeContext)this);

      // case 1: intermediate node
      public CopyOnAddDictionary<int, VoxTypeTrieNode> TypeIdToChildNode { get; } = new();

      // case 2: terminal node - late initialized
      public ManualResetEvent LateInitializeLatch { get; } = new(false);
      public Type? CompleteTypeOrNull { get; private set; } // Late-initialized immediately after construction. For array T[], is T[]
      public Type CompleteTypeInfoRoot { get; private set; } // For array T[], is RuntimePolymorphicArray1<>
      public Type[] CompleteTypeInfoArgs { get; private set; } // For array T[], is [T]

      /// <summary>
      /// for terminal nodes, this is cached. The source of truth the
      /// <seealso cref="VoxSerializerContainer"/>
      /// </summary>
      public IVoxSerializer? SerializerCacheOrNull;

      internal void LateInitializeTerminalNode(Type completeType, Type vtuInfoRoot, Type[] vtuInfoArgs) {
         CompleteTypeOrNull = completeType.AssertIsNotNull();
         CompleteTypeInfoRoot = vtuInfoRoot;
         CompleteTypeInfoArgs = vtuInfoArgs;
         LateInitializeLatch.Set();
      }

      public void WaitForLateInitialize() {
         LateInitializeLatch.WaitOne();
         CompleteTypeOrNull.AssertIsNotNull();
      }
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
         [typeof(VoxTypePlaceholders.RuntimePolymorphicArray1<>)] = typeof(RuntimePolymorphicArray1Serializer<>),
         [typeof(List<>)] = typeof(RuntimePolymorphicListSerializer<>),
         [typeof(Dictionary<,>)] = typeof(RuntimePolymorphicDictionarySerializer<,>),
      };

      public override List<Type> DependencyVoxTypes { get; } = new();
   }

   public class TestVoxTypes : VoxTypes {
      public override List<Type> AutoserializedTypes { get; } = new() {
         typeof(SimpleTestType),
         typeof(HodgepodgeMin),
      };
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
      public string String { get; set; }
      public Guid Guid { get; set; }
      public int[] IntArray { get; set; }
      public List<int> IntList { get; set; }
      [D<N, D<P, N[]>[][]>] public Dictionary<int, Dictionary<object, int[]>[][]> DictOfIntToArrayOfArrayOfDictOfStringToIntArray { get; set; }
      [P] public object PolymorphicString { get; set; }
      [P] public object PolymorphicIntArray { get; set; }
      [P] public object PolymorphicIntList { get; set; }
      [P] public object PolymorphicIntToIntArrayDict { get; set; }
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

   // public static class VoxReflectionCache_Helpers {
   //    public static byte[] Array1TypeIdBytes = ((int)BuiltInVoxTypeIds.Array1).ToVariableIntBytes();
   // }
   //
   // public static class VoxReflectionCache {
   //    public static void HintSerializer<T>(IVoxSerializer<T> inst) {
   //       VoxReflectionCache<T>.HintSerializer(inst);
   //    }
   // }
   //
   // /// <summary>
   // /// Reflection cache, primarily for looking up the serializer of a given type.
   // /// Note: Types' serializers cannot rely upon ReflectionCache to provide their typeId/typeIdBytes,
   // /// as such values are derived from the TypeSerializer instance (otherwise we'd have a circular
   // /// dependency).
   // /// </summary>
   // /// <typeparam name="T"></typeparam>
   // // ReSharper disable StaticMemberInGenericType
   // public static class VoxReflectionCache<T> {
   //    private static byte[]? typeIdBytes;
   //    private static IVoxSerializer<T>? serializer;
   //
   //    static VoxReflectionCache() {
   //       if (typeof(T).IsAssignableTo(typeof(IVoxCustomType<T>))) {
   //          var tInst = (IVoxCustomType<T>)(object)Activator.CreateInstance<T>()!;
   //          serializer = tInst.Serializer;
   //       }
   //    }
   //
   //    /// <summary>
   //    /// Informs the reflection cache that the given serializer instance exists.
   //    /// This matters because serializer constructors are recursive; a serializer
   //    /// constructor of a type might request the serializer of field's type. To
   //    /// make these calls succeed, serializers should invoke <see cref="HintSerializer"/>
   //    /// as their first operation.
   //    /// </summary>
   //    /// <param name="inst"></param>
   //    public static void HintSerializer(IVoxSerializer<T> inst) => serializer ??= inst;
   //
   //    // private static void EnsureInitialized_HintTIsListlikeWithElementType<TElement>() {
   //    //    var elementTypeIdBytes = VoxReflectionCache<TElement>.GetTypeIdBytes();
   //    //    var elementSerializer = VoxReflectionCache<TElement>.GetSerializer();
   //    //    var listLikeTypeIdBytes = VoxReflectionCache_Helpers.ListLikeTypeIdBytes;
   //    //    var res = new byte[listLikeTypeIdBytes.Length + elementTypeIdBytes.Length];
   //    //    listLikeTypeIdBytes.AsSpan().CopyTo(res);
   //    //    elementTypeIdBytes.AsSpan().CopyTo(res.AsSpan(listLikeTypeIdBytes.Length));
   //    //    typeIdBytes = res;
   //    //    serializer = (IVoxSerializer<T>)(object)ArraySerializer<TElement>.Instance;
   //    // }
   //
   //    // public static byte[] GetTypeIdBytes() {
   //    //
   //    // }
   //
   //    public static IVoxSerializer<T> GetSerializer() => serializer.AssertIsNotNull();
   // }

   [VoxType((int)BuiltInVoxTypeIds.Array1, Flags = VoxTypeFlags.NonUpdatable | VoxTypeFlags.NoCodeGen, VanityRedirectFromType = typeof(Array))]
   public class RuntimePolymorphicArray1Serializer<T> : IVoxSerializer<T[]> {
      private static class Constants {
         public const int SimpleTypeId = (int)BuiltInVoxTypeIds.Array1;
         public static readonly byte[] SimpleTypeIdBytes = SimpleTypeId.ToVariableIntBytes();
      }

      private readonly IVoxSerializer<T> elementSerializer;

      public RuntimePolymorphicArray1Serializer(VoxContext vc) {
         elementSerializer = vc.SerializerContainer.GetSerializerForType<T>();
         FullTypeId = Arrays.Concat(Constants.SimpleTypeId, elementSerializer.FullTypeId);
         FullTypeIdBytes = Arrays.Concat(Constants.SimpleTypeIdBytes, elementSerializer.FullTypeIdBytes);
      }

      public int SimpleTypeId => Constants.SimpleTypeId;
      public int[] FullTypeId { get; }
      public byte[] FullTypeIdBytes { get; }
      public bool IsUpdatable => false;

      public void WriteRawObject(VoxWriter writer, object val) {
         var x = (T[])val;
         WriteRaw(writer, ref x);
      }

      public object ReadRawObject(VoxReader reader) => ReadRaw(reader);

      public void WriteFull(VoxWriter writer, ref T[] val) {
         writer.WriteTypeIdBytes(FullTypeIdBytes);
         WriteRaw(writer, ref val);
      }

      public void WriteRaw(VoxWriter writer, ref T[] val) {
         writer.InnerWriter.Write((int)val.Length);
         for (var i = 0; i < val.Length; i++) {
            elementSerializer.WriteFull(writer, ref val[i]);
         }
      }

      public T[] ReadFull(VoxReader reader) {
         reader.AssertReadTypeIdBytes(FullTypeIdBytes);
         return ReadRaw(reader);
      }

      public T[] ReadRaw(VoxReader reader) {
         var len = reader.InnerReader.ReadInt32();
         var res = new T[len];
         for (var i = 0; i < res.Length; i++) {
            res[i] = elementSerializer.ReadFull(reader);
         }
         return res;
      }

      public void ReadFullIntoRef(VoxReader reader, ref T[] val)
         => throw new NotSupportedException();

      public void ReadRawIntoRef(VoxReader reader, ref T[] val)
         => throw new NotSupportedException();
   }

   [VoxType((int)BuiltInVoxTypeIds.List, Flags = VoxTypeFlags.NonUpdatable | VoxTypeFlags.NoCodeGen, VanityRedirectFromType = typeof(List<>))]
   public class RuntimePolymorphicListSerializer<T> : IVoxSerializer<List<T>> {
      private static class Constants {
         public const int SimpleTypeId = (int)BuiltInVoxTypeIds.List;
         public static readonly byte[] SimpleTypeIdBytes = SimpleTypeId.ToVariableIntBytes();
      }

      private readonly IVoxSerializer<T> elementSerializer;

      public RuntimePolymorphicListSerializer(VoxContext vc) {
         elementSerializer = vc.SerializerContainer.GetSerializerForType<T>();
         FullTypeId = Arrays.Concat(Constants.SimpleTypeId, elementSerializer.FullTypeId);
         FullTypeIdBytes = Arrays.Concat(Constants.SimpleTypeIdBytes, elementSerializer.FullTypeIdBytes);
      }

      public int SimpleTypeId => Constants.SimpleTypeId;
      public int[] FullTypeId { get; }
      public byte[] FullTypeIdBytes { get; }
      public bool IsUpdatable => false;

      public void WriteRawObject(VoxWriter writer, object val) {
         var x = (List<T>)val;
         WriteRaw(writer, ref x);
      }

      public object ReadRawObject(VoxReader reader) => ReadRaw(reader);

      public void WriteFull(VoxWriter writer, ref List<T> val) {
         writer.WriteTypeIdBytes(FullTypeIdBytes);
         WriteRaw(writer, ref val);
      }

      public void WriteRaw(VoxWriter writer, ref List<T> val) {
         writer.InnerWriter.Write((int)val.Count);
         for (var i = 0; i < val.Count; i++) {
            var el = val[i]; // unfortunate struct copy
            elementSerializer.WriteFull(writer, ref el);
         }
      }

      public List<T> ReadFull(VoxReader reader) {
         reader.AssertReadTypeIdBytes(FullTypeIdBytes);
         return ReadRaw(reader);
      }

      public List<T> ReadRaw(VoxReader reader) {
         var len = reader.InnerReader.ReadInt32();
         var res = new List<T>(len);
         for (var i = 0; i < len; i++) {
            res.Add(elementSerializer.ReadFull(reader));
         }
         return res;
      }

      public void ReadFullIntoRef(VoxReader reader, ref List<T> val)
         => throw new NotSupportedException();

      public void ReadRawIntoRef(VoxReader reader, ref List<T> val)
         => throw new NotSupportedException();
   }

   [VoxType((int)BuiltInVoxTypeIds.Dictionary, Flags = VoxTypeFlags.NonUpdatable | VoxTypeFlags.NoCodeGen, VanityRedirectFromType = typeof(Dictionary<,>))]
   public class RuntimePolymorphicDictionarySerializer<K, V> : IVoxSerializer<Dictionary<K, V>> {
      private static class Constants {
         public const int SimpleTypeId = (int)BuiltInVoxTypeIds.Dictionary;
         public static readonly byte[] SimpleTypeIdBytes = SimpleTypeId.ToVariableIntBytes();
      }

      private readonly IVoxSerializer<K> keySerializer;
      private readonly IVoxSerializer<V> valueSerializer;

      public RuntimePolymorphicDictionarySerializer(VoxContext vc) {
         keySerializer = vc.SerializerContainer.GetSerializerForType<K>();
         valueSerializer = vc.SerializerContainer.GetSerializerForType<V>();
         FullTypeId = Arrays.Concat(Constants.SimpleTypeId, keySerializer.FullTypeId, valueSerializer.FullTypeId);
         FullTypeIdBytes = Arrays.Concat(Constants.SimpleTypeIdBytes, keySerializer.FullTypeIdBytes, valueSerializer.FullTypeIdBytes);
      }

      public int SimpleTypeId => Constants.SimpleTypeId;
      public int[] FullTypeId { get; }
      public byte[] FullTypeIdBytes { get; }
      public bool IsUpdatable => false;

      public void WriteRawObject(VoxWriter writer, object val) {
         var x = (Dictionary<K, V>)val;
         WriteRaw(writer, ref x);
      }

      public object ReadRawObject(VoxReader reader) => ReadRaw(reader);

      public void WriteFull(VoxWriter writer, ref Dictionary<K, V> val) {
         writer.WriteTypeIdBytes(FullTypeIdBytes);
         WriteRaw(writer, ref val);
      }

      public void WriteRaw(VoxWriter writer, ref Dictionary<K, V> val) {
         writer.InnerWriter.Write((int)val.Count);
         foreach (var x in val) {
            var key = x.Key;
            var value = x.Value;
            keySerializer.WriteFull(writer, ref key);
            valueSerializer.WriteFull(writer, ref value);
         }
      }

      public Dictionary<K, V> ReadFull(VoxReader reader) {
         reader.AssertReadTypeIdBytes(FullTypeIdBytes);
         return ReadRaw(reader);
      }

      public Dictionary<K, V> ReadRaw(VoxReader reader) {
         var len = reader.InnerReader.ReadInt32();
         var res = new Dictionary<K, V>(len);
         for (var i = 0; i < len; i++) {
            var key = keySerializer.ReadFull(reader);
            var value = valueSerializer.ReadFull(reader);
            res.Add(key, value);
         }
         return res;
      }

      public void ReadFullIntoRef(VoxReader reader, ref Dictionary<K, V> val)
         => throw new NotSupportedException();

      public void ReadRawIntoRef(VoxReader reader, ref Dictionary<K, V> val)
         => throw new NotSupportedException();
   }

   public class Animal {}
   public class Dog : Animal {}
   public class Cat : Animal {}
}