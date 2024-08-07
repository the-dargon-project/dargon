﻿using System.Diagnostics;
using System.Numerics;
using System.Text;
using Dargon.Commons;
using Dargon.Commons.Collections;
using Dargon.Commons.Exceptions;
using Dargon.Commons.Utilities;

namespace Dargon.Vox2 {
   public class Program {
      static void Main(string[] args) {
         Console.WriteLine("Hello, World!");

         var hodgepodgeOriginal = new HodgepodgeMin {
            True = true,
            False = false,
            PolymorphicTrue = true,
            PolymorphicFalse = false,

            Int8 = 10,
            Int16 = 1234,
            Int32 = 213812912,
            Int64 = long.MaxValue,

            UInt8 = 213,
            UInt16 = 51123,
            UInt32 = 3123423432,
            UInt64 = ulong.MaxValue,

            NullString = null,
            String = "Hello, World!",
            Guid = new Guid(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1),
            IntArray = new int[] { 1, 2, 3 },
            IntList = new() { 21, 22, 23 },
            IntHashSet = new() { 1, 2, 3, 4 },
            DictOfIntToArrayOfArrayOfDictOfObjectToIntArray = new() {
               [123] = new Dictionary<object, int[]>[][] {
                  new Dictionary<object, int[]>[] {
                     new Dictionary<object, int[]> {
                        ["Key1"] = new int[] { 1, 2, 3, 4, },
                        ["Key2"] = new int[] { 11, 22, 33, 44, },
                     },
                     new Dictionary<object, int[]> {
                        ["Key3"] = new int[] { 111, 222, 333, 444, },
                        ["Key4"] = null,
                     },
                  },
                  new Dictionary<object, int[]>[0],
                  null,
               },
               [234] = new Dictionary<object, int[]>[0][],
            },
            NullIntArray = null,
            NullIntList = null,
            NullIntHashSet = null,
            NullDictOfIntToArrayOfArrayOfDictOfObjectToIntArray = null,
            PolymorphicNull = null,
            PolymorphicString = "abc",
            PolymorphicIntArray = new int[] { 123, 456, 789 },
            PolymorphicIntList = new List<int> { 31, 32, 32 },
            PolymorphicIntHashSet = new HashSet<int> { 1, 2, 3, 4 },
            PolymorphicIntToIntArrayDict = new Dictionary<int, int[]> {
               [420] = new[] { 81423, 17 },
            },
            Tuple = (123, "Hello, World!"),
            TypeInt = typeof(int),
            TypeIntArray = typeof(int[]),
            TypeDictOfIntToArrayOfArrayOfDictOfObjectToIntArray = typeof(Dictionary<int, Dictionary<object, int>[][]>),
            TypeStringSplitOptions = typeof(StringSplitOptions),
            NullType = null,
            PolymorphicType = typeof(Dictionary<int, HashSet<string>>),

            Vector2 = new(1, 2),
            Vector3 = new(1, 2, 3),
            Vector4 = new(1, 2, 3, 4),

            NullNullableVector2 = null,
            NonNullNullableVector2 = new Vector2(1, 2),

            DateTime = DateTime.Now,
            DateTimeOffset = DateTimeOffset.Now,
            TimeSpan = TimeSpan.FromSeconds(12345.678),

            Inner = new() {
               Inner = null,
            },

            IntBox = new GenericBox<int> {
               V1 = 123,
               V2 = 234,
            },
            BoolBox = new GenericBox<bool> {
               V1 = false,
               V2 = true,
            },
            IntListBox = new GenericBox<List<int>> {
               V1 = new List<int> { 1, 2, 3 },
               V2 = new List<int> { 2, 3, 4, 5, 6 },
            },
            BoolListBox = new GenericBox<List<bool>> {
               V1 = new List<bool> { false, true, false, true },
               V2 = new List<bool> { false, true, false, true, false, true },
            },
            PolymorphicIntBox = new GenericBox<int> {
               V1 = 123,
               V2 = 234,
            },
            PolymorphicBoolBox = new GenericBox<bool> {
               V1 = true,
               V2 = false,
            },
            PolymorphicIntListBox = new GenericBox<List<int>> {
               V1 = new List<int> { 1, 2, 3 },
               V2 = new List<int> { 2, 3, 4, 5, 6 },
            },
            PolymorphicBoolListBox = new GenericBox<List<bool>> {
               V1 = new List<bool> { false, true, true, true, false },
               V2 = new List<bool> { false, true, true, true, false, true, false },
            },

            IntStructBox = new GenericStructBox<int> { V1 = 1234 },
            IntListStructBox = new GenericStructBox<List<int>> {
               V1 = new List<int> { 123, 456, 789 },
            },
            NullIntListStructBox = default,

            StringSplitOptions_None = StringSplitOptions.None,
            StringSplitOptions_RemoveEmptyEntries = StringSplitOptions.RemoveEmptyEntries,
            PolymorphicStringSplitOptions = StringSplitOptions.TrimEntries,

            InnerClassBox2 = new TestParent.GenericBox2<int, string> {
               V1 = 123,
               V2 = 234,
               V3 = "asdf",
               V4 = "qwer",
            },

            InnerClassBox3 = new TestParent.GenericBox3<bool, int, string> {
               V1 = false,
               V2 = true,
               V3 = 123,
               V4 = 234,
               V5 = "asdf",
               V6 = "qwer",
            },

            InnerClassBox4 = new TestParent.GenericBox4<bool, int, string> {
               A = null,
               B = new TestParent.GenericBox3<int, string, bool> {
                  V1 = 123,
                  V2 = 234,
                  V3 = "eerq",
                  V4 = "q14",
                  V5 = false,
                  V6 = true,
               },
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

         hodgepodgeOriginal.True.AssertEquals(rt.True);
         hodgepodgeOriginal.False.AssertEquals(rt.False);
         hodgepodgeOriginal.PolymorphicTrue.AssertEquals(rt.PolymorphicTrue);
         hodgepodgeOriginal.PolymorphicFalse.AssertEquals(rt.PolymorphicFalse);

         hodgepodgeOriginal.Int8.AssertEquals(rt.Int8);
         hodgepodgeOriginal.Int16.AssertEquals(rt.Int16);
         hodgepodgeOriginal.Int32.AssertEquals(rt.Int32);
         hodgepodgeOriginal.Int64.AssertEquals(rt.Int64);
         hodgepodgeOriginal.UInt8.AssertEquals(rt.UInt8);
         hodgepodgeOriginal.UInt16.AssertEquals(rt.UInt16);
         hodgepodgeOriginal.UInt32.AssertEquals(rt.UInt32);
         hodgepodgeOriginal.UInt64.AssertEquals(rt.UInt64);
         hodgepodgeOriginal.NullString.AssertIsNull();
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

         hodgepodgeOriginal.IntHashSet.Count.AssertEquals(rt.IntHashSet.Count);
         foreach (var x in hodgepodgeOriginal.IntHashSet) {
            rt.IntHashSet.Contains(x).AssertIsTrue();
         }

         var dictOrig = hodgepodgeOriginal.DictOfIntToArrayOfArrayOfDictOfObjectToIntArray;
         var dictRead = rt.DictOfIntToArrayOfArrayOfDictOfObjectToIntArray;
         var codeOrig = dictOrig.ToCodegenDump();
         var codeRead = dictRead.ToCodegenDump();
         codeOrig.AssertEquals(codeRead);

         hodgepodgeOriginal.NullIntArray.AssertIsNull();
         hodgepodgeOriginal.NullIntList.AssertIsNull();
         hodgepodgeOriginal.NullIntHashSet.AssertIsNull();
         hodgepodgeOriginal.NullDictOfIntToArrayOfArrayOfDictOfObjectToIntArray.AssertIsNull();

         rt.PolymorphicNull.AssertIsNull();
         hodgepodgeOriginal.PolymorphicString.AssertEquals(rt.PolymorphicString);
         
         var hoPIA = (int[])hodgepodgeOriginal.PolymorphicIntArray;
         var rtPIA = (int[])rt.PolymorphicIntArray;
         hoPIA.Length.AssertEquals(rtPIA.Length);
         foreach (var (i, x) in hoPIA.Enumerate()) {
            rtPIA[i].AssertEquals(x);
         }

         var hoPIL = (List<int>)hodgepodgeOriginal.PolymorphicIntList;
         var rtPIL = (List<int>)rt.PolymorphicIntList;
         hoPIL.Count.AssertEquals(rtPIL.Count);
         foreach (var (i, x) in hoPIL.Enumerate()) {
            rtPIL[i].AssertEquals(x);
         }

         var hoPIHS = (HashSet<int>)hodgepodgeOriginal.PolymorphicIntHashSet;
         var rtPIHS = (HashSet<int>)rt.PolymorphicIntHashSet;
         hoPIHS.Count.AssertEquals(rtPIHS.Count);
         foreach (var x in hoPIHS) {
            rtPIHS.Contains(x).AssertIsTrue();
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


         hodgepodgeOriginal.Tuple.AssertEquals(rt.Tuple);
         hodgepodgeOriginal.TypeInt.AssertEquals(rt.TypeInt);
         hodgepodgeOriginal.TypeIntArray.AssertEquals(rt.TypeIntArray);
         hodgepodgeOriginal.TypeStringSplitOptions.AssertEquals(rt.TypeStringSplitOptions);
         hodgepodgeOriginal.TypeDictOfIntToArrayOfArrayOfDictOfObjectToIntArray.AssertEquals(rt.TypeDictOfIntToArrayOfArrayOfDictOfObjectToIntArray);
         hodgepodgeOriginal.NullType.AssertEquals(rt.NullType);
         hodgepodgeOriginal.PolymorphicType.AssertEquals(rt.PolymorphicType);
         hodgepodgeOriginal.Vector2.AssertEquals(rt.Vector2);
         hodgepodgeOriginal.Vector3.AssertEquals(rt.Vector3);
         hodgepodgeOriginal.Vector4.AssertEquals(rt.Vector4);
         hodgepodgeOriginal.NullNullableVector2.AssertIsNull();
         hodgepodgeOriginal.NonNullNullableVector2.AssertEquals(rt.NonNullNullableVector2!.Value);

         hodgepodgeOriginal.DateTime.AssertEquals(rt.DateTime);
         hodgepodgeOriginal.DateTimeOffset.AssertEquals(rt.DateTimeOffset);
         hodgepodgeOriginal.TimeSpan.AssertEquals(rt.TimeSpan);

         rt.Inner.Inner.AssertIsNull();

         hodgepodgeOriginal.IntBox.V1.AssertEquals(rt.IntBox.V1);
         hodgepodgeOriginal.IntBox.V2.AssertEquals(rt.IntBox.V2);
         hodgepodgeOriginal.BoolBox.V1.AssertEquals(rt.BoolBox.V1);
         hodgepodgeOriginal.BoolBox.V2.AssertEquals(rt.BoolBox.V2);
         hodgepodgeOriginal.IntListBox.V1.Count.AssertEquals(rt.IntListBox.V1.Count);
         hodgepodgeOriginal.IntListBox.V2.Count.AssertEquals(rt.IntListBox.V2.Count);
         hodgepodgeOriginal.BoolListBox.V1.ToCodegenDump().AssertEquals(rt.BoolListBox.V1.ToCodegenDump());
         hodgepodgeOriginal.BoolListBox.V2.ToCodegenDump().AssertEquals(rt.BoolListBox.V2.ToCodegenDump());
         ((GenericBox<int>)hodgepodgeOriginal.PolymorphicIntBox).V1.AssertEquals(((GenericBox<int>)rt.PolymorphicIntBox).V1);
         ((GenericBox<int>)hodgepodgeOriginal.PolymorphicIntBox).V2.AssertEquals(((GenericBox<int>)rt.PolymorphicIntBox).V2);
         ((GenericBox<bool>)hodgepodgeOriginal.PolymorphicBoolBox).V1.AssertEquals(((GenericBox<bool>)rt.PolymorphicBoolBox).V1);
         ((GenericBox<bool>)hodgepodgeOriginal.PolymorphicBoolBox).V2.AssertEquals(((GenericBox<bool>)rt.PolymorphicBoolBox).V2);
         ((GenericBox<List<int>>)hodgepodgeOriginal.PolymorphicIntListBox).V1.ToCodegenDump().AssertEquals(((GenericBox<List<int>>)rt.PolymorphicIntListBox).V1.ToCodegenDump());
         ((GenericBox<List<int>>)hodgepodgeOriginal.PolymorphicIntListBox).V2.ToCodegenDump().AssertEquals(((GenericBox<List<int>>)rt.PolymorphicIntListBox).V2.ToCodegenDump());
         ((GenericBox<List<bool>>)hodgepodgeOriginal.PolymorphicBoolListBox).V1.ToCodegenDump().AssertEquals(((GenericBox<List<bool>>)rt.PolymorphicBoolListBox).V1.ToCodegenDump());
         ((GenericBox<List<bool>>)hodgepodgeOriginal.PolymorphicBoolListBox).V2.ToCodegenDump().AssertEquals(((GenericBox<List<bool>>)rt.PolymorphicBoolListBox).V2.ToCodegenDump());

         hodgepodgeOriginal.IntStructBox.V1.AssertEquals(rt.IntStructBox.V1);
         hodgepodgeOriginal.IntListStructBox.V1.ToCodegenDump().AssertEquals(rt.IntListStructBox.V1.ToCodegenDump());
         hodgepodgeOriginal.NullIntListStructBox.V1.ToCodegenDump().AssertEquals(rt.NullIntListStructBox.V1.ToCodegenDump());

         hodgepodgeOriginal.StringSplitOptions_None.AssertEquals(rt.StringSplitOptions_None);
         hodgepodgeOriginal.StringSplitOptions_RemoveEmptyEntries.AssertEquals(rt.StringSplitOptions_RemoveEmptyEntries);
         hodgepodgeOriginal.PolymorphicStringSplitOptions.AssertEquals(rt.PolymorphicStringSplitOptions);

         hodgepodgeOriginal.InnerClassBox2.V1.AssertEquals(rt.InnerClassBox2.V1);
         hodgepodgeOriginal.InnerClassBox2.V2.AssertEquals(rt.InnerClassBox2.V2);
         hodgepodgeOriginal.InnerClassBox2.V3.AssertEquals(rt.InnerClassBox2.V3);
         hodgepodgeOriginal.InnerClassBox2.V4.AssertEquals(rt.InnerClassBox2.V4);

         hodgepodgeOriginal.InnerClassBox3.V1.AssertEquals(rt.InnerClassBox3.V1);
         hodgepodgeOriginal.InnerClassBox3.V2.AssertEquals(rt.InnerClassBox3.V2);
         hodgepodgeOriginal.InnerClassBox3.V3.AssertEquals(rt.InnerClassBox3.V3);
         hodgepodgeOriginal.InnerClassBox3.V4.AssertEquals(rt.InnerClassBox3.V4);
         hodgepodgeOriginal.InnerClassBox3.V5.AssertEquals(rt.InnerClassBox3.V5);
         hodgepodgeOriginal.InnerClassBox3.V6.AssertEquals(rt.InnerClassBox3.V6);

         hodgepodgeOriginal.InnerClassBox4.A.AssertEquals(rt.InnerClassBox4.A).AssertIsNull();
         hodgepodgeOriginal.InnerClassBox4.B.V1.AssertEquals(rt.InnerClassBox4.B.V1);
         hodgepodgeOriginal.InnerClassBox4.B.V2.AssertEquals(rt.InnerClassBox4.B.V2);
         hodgepodgeOriginal.InnerClassBox4.B.V3.AssertEquals(rt.InnerClassBox4.B.V3);
         hodgepodgeOriginal.InnerClassBox4.B.V4.AssertEquals(rt.InnerClassBox4.B.V4);
         hodgepodgeOriginal.InnerClassBox4.B.V5.AssertEquals(rt.InnerClassBox4.B.V5);
         hodgepodgeOriginal.InnerClassBox4.B.V6.AssertEquals(rt.InnerClassBox4.B.V6);

         ms.Position.AssertEquals(writeLen);

         ms.Position = 0;
         var hpso = new HodgepodgeStructMin {
            I32 = 123,
            Inner = new() {
               DateTime = DateTime.Now,
            },
         };
         vw.WritePolymorphic(hpso);
         writeLen = ms.Position;
         ms.Position = 0;
         var hpsr = (HodgepodgeStructMin)vr.ReadPolymorphic();
         hpso.I32.AssertEquals(hpsr.I32);
         hpso.Inner.DateTime.AssertEquals(hpsr.Inner.DateTime);
         ms.Position.AssertEquals(writeLen);
      }
   }

   public interface IPolymorphicSerializer {
      Type ReadFullType(VoxReader reader);
      T ReadPolymorphicFull<T>(VoxReader reader);

      void WriteFullType(VoxWriter writer, Type type);
      void WritePolymorphicFull<T>(VoxWriter writer, ref T x);
   }

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

   public class VoxWriter : IDisposable {
      private BinaryWriter bw;
      private VoxContext voxContext;

      public VoxWriter(Stream s, VoxContext voxContext, bool leaveOpen = true) : this(new BinaryWriter(s, Encoding.UTF8, leaveOpen), voxContext) { }

      public VoxWriter(BinaryWriter bw, VoxContext voxContext) {
         this.bw = bw;
         this.voxContext = voxContext;
      }

      public VoxContext Context => voxContext;

      public BinaryWriter InnerWriter
      {
         get
         {
            if (bw == null) throw new ObjectDisposedException(nameof(VoxWriter));
            return bw;
         }
      }

      public void WriteTypeIdBytes(byte[] data) => InnerWriter.Write(data);

      public void WriteRawBytes(byte[] data) => InnerWriter.Write(data);

      public void WriteFullType(Type type) {
         voxContext.PolymorphicSerializer.WriteFullType(this, type);
      }

      public void WritePolymorphic<T>(T inst) {
         // if (typeof(T).IsValueType) throw new ArgumentException($"{typeof(T).FullName} is a value type.");
         voxContext.PolymorphicSerializer.WritePolymorphicFull(this, ref inst);
      }

      public void Dispose() {
         bw?.Dispose();
         bw = null;
      }
   }

   public class VoxReader : IDisposable {
      private BinaryReader br;
      private VoxContext voxContext;

      public VoxReader(Stream s, VoxContext voxContext, bool leaveOpen = true) : this(new BinaryReader(s, Encoding.UTF8, leaveOpen), voxContext) { }

      public VoxReader(BinaryReader br, VoxContext voxContext) {
         this.br = br;
         this.voxContext = voxContext;
      }
      
      public VoxContext Context => voxContext;

      public BinaryReader InnerReader
      {
         get
         {
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
         return voxContext.PolymorphicSerializer.ReadFullType(this);
      }

      public object ReadPolymorphic() => ReadPolymorphic<object>();

      public T ReadPolymorphic<T>() {
         // if (typeof(T).IsValueType) throw new ArgumentException($"{typeof(T).FullName} is a value type.");
         return voxContext.PolymorphicSerializer.ReadPolymorphicFull<T>(this);
      }

      public void Dispose() {
         br?.Dispose();
         br = null;
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
         try {
            simpleTypeToTrieRootContext.Add(c.SimpleType, c);
            simpleTypeIdToTrieRootContext.Add(c.SimpleTypeId, c);
            if (c.CompleteTypeOrNull is { } ct) {
               completeTypeToNodeCache.AddOrThrow(ct, c);
            }
         } catch (Exception e) {
            throw new VoxTypeImportException(c, e);
         }
      }

      public void ImportTypeIdAliasedContext(VoxTypeContext c) {
         simpleTypeIdToTrieRootContext.Add(c.SimpleTypeId, c);
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

         (t.IsArray || t.IsGenericType).AssertIsTrue();

         // case 2: t is a complete generic type - visit its generic type definition, then args
         var (tGtd, tArgs) = VoxTypeUtils.UnpackTypeToVoxRootTypeAndArgs(t);
         current = GetOrCreateTrieNodeOfCompleteType_RecurseToTrieLeaf(tGtd, current);
         foreach (var tArg in tArgs) {
            current = GetOrCreateTrieNodeOfCompleteType_RecurseToTrieLeaf(tArg, current);
         }
         return current;
      }
   }

   public class VoxTypeImportException(VoxTypeContext VoxTypeContext, Exception InnerException)
      : Exception($"Failed to import {VoxTypeContext.SimpleType} ({VoxTypeContext.SimpleTypeId}) with serializer {VoxTypeContext.SerializerType}", InnerException);

   public static class VoxContextFactory {
      public static VoxContext Create(VoxTypes voxTypes) {
         var res = new VoxContext { VoxTypes = voxTypes };
         var trieContainer = res.TrieContainer = new VoxTypeTrieContainer();
         var serializerContainer = res.SerializerContainer = new VoxSerializerContainer(res, trieContainer);
         var polymorphicSerializer = new PolymorphicSerializerImpl(trieContainer, serializerContainer);
         res.PolymorphicSerializer = polymorphicSerializer;

         var seenVoxTypes = new HashSet<VoxTypes>();

         void Visit(VoxTypes vt) {
            if (!seenVoxTypes.Add(vt)) {
               return;
            }

            var typeToSerializer = new Dictionary<Type, Type>(vt.TypeToCustomSerializers);
            foreach (var t in vt.AutoserializedTypes) {
               var attr = t.GetAttributeOrNull<VoxInternalsAutoSerializedTypeInfoAttribute>().AssertIsNotNull();
               var serializerType = attr.GenericSerializerTypeDefinition.AssertIsNotNull();
               typeToSerializer.Add(t, serializerType);
            }

            foreach (var (tseri, t) in vt.TypeIdAliasedSerializers) {
               var seri = (IVoxSerializer)Activator.CreateInstance(tseri);
               var vtc = new VoxTypeContext(seri.SimpleTypeId, null) {
                  SerializerCacheOrNull = seri,
                  SerializerType = tseri,
                  Internal_Type_GenericArguments_Cache = Array.Empty<Type>(),
               };
               vtc.LateInitializeTerminalNode(t, t, Type.EmptyTypes);
               trieContainer.ImportTypeIdAliasedContext(vtc);
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

      public Type? ReadFullType(VoxReader reader) {
         var t = ReadFullTypeInternal(reader).CompleteTypeOrNull.AssertIsNotNull();
         if (t == typeof(VoxTypePlaceholders.RuntimePolymorphicNull)) return null;
         return t;
      }

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
         if (type == null) type = typeof(VoxTypePlaceholders.RuntimePolymorphicNull);
         var tn = trieContainer.GetOrCreateTrieNodeOfCompleteType(type);
         var seri = serializerContainer.GetSerializerForTrieNode(tn);
         writer.WriteTypeIdBytes(seri.FullTypeIdBytes);
      }

      public void WritePolymorphicFull<T>(VoxWriter writer, ref T x) {
         if (x == null) {
            var nullSerializer = serializerContainer.GetSerializerForType(typeof(VoxTypePlaceholders.RuntimePolymorphicNull));
            writer.WriteTypeIdBytes(nullSerializer.FullTypeIdBytes);
            return;
         }

         var xType = x is Type ? typeof(Type) : x.GetType(); // HACK for polymorphic serialization of Type variants.
         var tn = trieContainer.GetOrCreateTrieNodeOfCompleteType(xType);
         var seri = serializerContainer.GetSerializerForTrieNode(tn);
         writer.WriteTypeIdBytes(seri.FullTypeIdBytes);
         seri.WriteRawObject(writer, x);
      }
   }

   public class VoxContext {
      internal VoxContext() {}

      public VoxTypes VoxTypes { get; set; }
      public VoxTypeTrieContainer TrieContainer { get; set; }
      public IPolymorphicSerializer PolymorphicSerializer { get; set; }
      public VoxSerializerContainer SerializerContainer { get; set; }

      public VoxWriter CreateWriter(Stream s, bool leaveOpen = true) => new(s, this, leaveOpen);
      public VoxReader CreateReader(Stream s, bool leaveOpen = true) => new(s, this, leaveOpen);

      public static VoxContext Create(VoxTypes voxTypes) => VoxContextFactory.Create(voxTypes);
   }

   public static class VoxContextExtensions {
      public static T RoundTrip<T>(this VoxContext vox, T val) {
         using var ms = new MemoryStream();
         using var w = vox.CreateWriter(ms);
         w.WritePolymorphic(val);
         ms.Position = 0;
         
         using var r = vox.CreateReader(ms);
         return r.ReadPolymorphic<T>();
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
      public abstract Dictionary<Type, Type> TypeToCustomSerializers { get; } // Type to Serializer
      public virtual Dictionary<Type, Type> TypeIdAliasedSerializers { get; } = new(); // Serializer to Type
      public abstract List<Type> DependencyVoxTypes { get; }

      public override int GetHashCode() => GetType().GetHashCode();
      public override bool Equals(object other) => Equals((VoxTypes)other);
      public bool Equals(VoxTypes other) => GetType() == other.GetType();
   }

   public class CoreVoxTypes : VoxTypes {
      public override List<Type> AutoserializedTypes { get; } = new();

      public override Dictionary<Type, Type> TypeToCustomSerializers { get; } = new() {
         [typeof(VoxTypePlaceholders.RuntimePolymorphicNull)] = typeof(RuntimePolymorphicNullSerializer),
         [typeof(Type)] = typeof(TypeSerializer),
         [typeof(Object)] = typeof(ObjectThrowAlwaysSerializer),
         [typeof(String)] = typeof(StringSerializer),

         [typeof(VoxTypePlaceholders.RuntimePolymorphicArray1<>)] = typeof(RuntimePolymorphicArray1Serializer<>),
         [typeof(List<>)] = typeof(RuntimePolymorphicListSerializer<>),
         [typeof(HashSet<>)] = typeof(RuntimePolymorphicHashSetSerializer<>),
         [typeof(Dictionary<,>)] = typeof(RuntimePolymorphicDictionarySerializer<,>),

         [typeof(bool)] = typeof(BoolVoxSerializer),

         [typeof(SByte)] = typeof(SByteSerializer),
         [typeof(Int16)] = typeof(Int16Serializer),
         [typeof(Int32)] = typeof(Int32Serializer),
         [typeof(Int64)] = typeof(Int64Serializer),
         [typeof(Byte)] = typeof(ByteSerializer),
         [typeof(UInt16)] = typeof(UInt16Serializer),
         [typeof(UInt32)] = typeof(UInt32Serializer),
         [typeof(UInt64)] = typeof(UInt64Serializer),
         [typeof(Single)] = typeof(SingleSerializer),
         [typeof(Double)] = typeof(DoubleSerializer),
         [typeof(Guid)] = typeof(GuidSerializer),
         
         [typeof(Vector2)] = typeof(Vector2Serializer),
         [typeof(Vector3)] = typeof(Vector3Serializer),
         [typeof(Vector4)] = typeof(Vector4Serializer),
         
         [typeof(DateTime)] = typeof(DateTimeSerializer),
         [typeof(DateTimeOffset)] = typeof(DateTimeOffsetSerializer),
         [typeof(TimeSpan)] = typeof(TimeSpanSerializer),
      };

      public override Dictionary<Type, Type> TypeIdAliasedSerializers { get; } = new() {
         [typeof(BoolTrueVoxSerializer)] = typeof(bool),
         [typeof(BoolFalseVoxSerializer)] = typeof(bool),
      };

      public override List<Type> DependencyVoxTypes { get; } = new();
   }

   public class TestVoxTypes : VoxTypes {
      public override List<Type> AutoserializedTypes { get; } = new() {
         typeof(SimpleTestType),
         typeof(HodgepodgeMin),
         typeof(HodgepodgeStructMin),
         typeof(HodgepodgeStructMin2),
         typeof(GenericBox<>),
         typeof(GenericStructBox<>),
         typeof(TestParent.GenericBox2<,>),
         typeof(TestParent.GenericBox3<,,>),
         typeof(TestParent.GenericBox4<,,>),
      };
      public override Dictionary<Type, Type> TypeToCustomSerializers { get; } = new() {
         [typeof(StringSplitOptions)] = typeof(StringSplitOptionsSerializer),
      };
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
      public bool True { get; set; }
      public bool False { get; set; }
      [P] public object PolymorphicTrue { get; set; }
      [P] public object PolymorphicFalse { get; set; }

      public sbyte Int8 { get; set; }
      public short Int16 { get; set; }
      public int Int32 { get; set; }
      public long Int64 { get; set; }
      
      public byte UInt8 { get; set; }
      public ushort UInt16 { get; set; }
      public uint UInt32 { get; set; }
      public ulong UInt64 { get; set; }

      public string? NullString { get; set; }
      public string String { get; set; }
      public Guid Guid { get; set; }
      
      public int[]? IntArray { get; set; }
      public List<int>? IntList { get; set; }
      public HashSet<int>? IntHashSet { get; set; }
      [D<N, D<P, N[]>[][]>] public Dictionary<int, Dictionary<object, int[]>[][]> DictOfIntToArrayOfArrayOfDictOfObjectToIntArray { get; set; }
      
      public int[]? NullIntArray { get; set; }
      public List<int>? NullIntList { get; set; }
      public HashSet<int>? NullIntHashSet { get; set; }
      [D<N, D<P, N[]>[][]>] public Dictionary<int, Dictionary<object, int[]>[][]>? NullDictOfIntToArrayOfArrayOfDictOfObjectToIntArray { get; set; }

      [P] public object? PolymorphicNull { get; set; }
      [P] public object PolymorphicString { get; set; }
      [P] public object PolymorphicIntArray { get; set; }
      [P] public object PolymorphicIntList { get; set; }
      [P] public object PolymorphicIntHashSet { get; set; }
      [P] public object PolymorphicIntToIntArrayDict { get; set; }
      
      public (int, string) Tuple { get; set; }

      public Type TypeInt { get; set; }
      public Type TypeIntArray { get; set; }
      public Type TypeDictOfIntToArrayOfArrayOfDictOfObjectToIntArray { get; set; }
      public Type TypeStringSplitOptions { get; set; }

      public Type? NullType { get; set; }
      [P] public object PolymorphicType { get; set; }
      
      public Vector2 Vector2 { get; set; }
      public Vector3 Vector3 { get; set; }
      public Vector4 Vector4 { get; set; }

      [P] public Vector2? NullNullableVector2 { get; set; }
      [P] public Vector2? NonNullNullableVector2 { get; set; }

      public DateTime DateTime { get; set; }
      public DateTimeOffset DateTimeOffset { get; set; }
      public TimeSpan TimeSpan { get; set; }

      [P] public HodgepodgeMin? Inner { get; set; } // recursive types must be declared polymorphic for now

      [P] public GenericBox<int>? IntBox;
      [P] public GenericBox<bool>? BoolBox;
      [P] public GenericBox<List<int>>? IntListBox;
      [P] public GenericBox<List<bool>>? BoolListBox;

      [P] public object? PolymorphicIntBox;
      [P] public object? PolymorphicBoolBox;
      [P] public object? PolymorphicIntListBox;
      [P] public object? PolymorphicBoolListBox;

      public GenericStructBox<int> IntStructBox;
      public GenericStructBox<List<int>> IntListStructBox;
      public GenericStructBox<List<int>> NullIntListStructBox;

      public StringSplitOptions StringSplitOptions_None;
      public StringSplitOptions StringSplitOptions_RemoveEmptyEntries;

      [P] public object? PolymorphicStringSplitOptions;

      [P] public TestParent.GenericBox2<int, string>? InnerClassBox2;
      [P] public TestParent.GenericBox3<bool, int, string>? InnerClassBox3;
      [P] public TestParent.GenericBox4<bool, int, string>? InnerClassBox4;

      public static void XX(HodgepodgeMin x) {
         // x.Tuple.Item1;
      }
   }

   [VoxType((int)BuiltInVoxTypeIds.ReservedForInternalVoxTest2)]
   public partial struct HodgepodgeStructMin {
      public int I32;
      public HodgepodgeStructMin2 Inner;
   }

   [VoxType((int)BuiltInVoxTypeIds.ReservedForInternalVoxTest3)]
   public partial struct HodgepodgeStructMin2 {
      public DateTime DateTime;
   }

   [VoxType((int)BuiltInVoxTypeIds.ReservedForInternalVoxTest4)]
   public partial class GenericBox<T> {
      public T V1;
      public T V2 { get; set; }
   }

   [VoxType((int)BuiltInVoxTypeIds.ReservedForInternalVoxTest5)]
   public partial struct GenericStructBox<T> {
      public T V1;
   }

   public static partial class TestParent {
      [VoxType((int)BuiltInVoxTypeIds.ReservedForInternalVoxTest7)]
      public partial class GenericBox2<T1, T2> : GenericBox<T1> {
         public T2 V3;
         public T2 V4 { get; set; }
      }

      [VoxType((int)BuiltInVoxTypeIds.ReservedForInternalVoxTest8)]
      public partial class GenericBox3<T1, T2, T3> : GenericBox2<T1, T2> {
         public T3 V5;
         public T3 V6 { get; set; }
      }

      [VoxType((int)BuiltInVoxTypeIds.ReservedForInternalVoxTest9)]
      public partial class GenericBox4<T1, T2, T3> {
         [P] public GenericBox2<T2, T3>? A;
         [N] public GenericBox3<T2, T3, T1> B;
      }
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
      public GenericBox<int> GenericBoxOfInt { get; set; }
   }

   // handles the case of serializing a bool
   [VoxType(BoolVoxSerializer.kSimpleTypeId, Flags = VoxTypeFlags.NoCodeGen, RedirectToType = typeof(bool))]
   public class BoolVoxSerializer : IVoxSerializer<bool> {
      public const int kSimpleTypeId = (int)BuiltInVoxTypeIds.Bool;
      public const int kTrueTypeId = (int)BuiltInVoxTypeIds.BoolTrue;
      public const int kFalseTypeId = (int)BuiltInVoxTypeIds.BoolFalse;

      private static readonly byte[] trueTypeIdBytes = kTrueTypeId.ToVariableIntBytes();
      private static readonly byte[] falseTypeIdBytes = kFalseTypeId.ToVariableIntBytes();

      static BoolVoxSerializer() {
         trueTypeIdBytes.Length.AssertEquals(1);
         falseTypeIdBytes.Length.AssertEquals(1);
      }

      public static readonly BoolVoxSerializer Instance = new BoolVoxSerializer();

      public int SimpleTypeId { get; } = kSimpleTypeId;
      public int[] FullTypeId { get; } = new[] { kSimpleTypeId };
      public byte[] FullTypeIdBytes { get; } = kSimpleTypeId.ToVariableIntBytes();
      public bool IsUpdatable => false;

      public void WriteRawObject(VoxWriter writer, object val) {
         var x = (bool)val;
         WriteRaw(writer, ref x);
      }

      public object ReadRawObject(VoxReader reader) => ReadRaw(reader);

      public void WriteFull(VoxWriter writer, ref bool val) => WriteRaw(writer, ref val);

      public void WriteRaw(VoxWriter writer, ref bool val)
         => writer.WriteTypeIdBytes(val ? trueTypeIdBytes : falseTypeIdBytes);

      public bool ReadFull(VoxReader reader) {
         var b = reader.InnerReader.ReadByte();
         if (b == trueTypeIdBytes[0]) return true;
         if (b == falseTypeIdBytes[0]) return false;
         throw new Exception($"?? Bool decode failed, unexpected {b}");
      }

      public bool ReadRaw(VoxReader reader) => ReadFull(reader);
      public void ReadFullIntoRef(VoxReader reader, ref bool val) => val = ReadFull(reader);
      public void ReadRawIntoRef(VoxReader reader, ref bool val) => val = ReadFull(reader);
   }

   /// <summary>
   /// Handles the case of reading a true
   /// </summary>
   public class BoolTrueVoxSerializer : IVoxSerializer<bool> {
      private const int kTrueTypeId = (int)BuiltInVoxTypeIds.BoolTrue;

      public int SimpleTypeId { get; } = kTrueTypeId;
      public int[] FullTypeId { get; } = new[] { kTrueTypeId };
      public byte[] FullTypeIdBytes { get; } = kTrueTypeId.ToVariableIntBytes();
      public bool IsUpdatable => false;

      public void WriteRawObject(VoxWriter writer, object val) { }
      public object ReadRawObject(VoxReader reader) => true;

      public void WriteFull(VoxWriter writer, ref bool val) {
         val.AssertIsTrue();
         writer.WriteTypeIdBytes(FullTypeIdBytes);
      }

      public void WriteRaw(VoxWriter writer, ref bool val) { }

      public bool ReadFull(VoxReader reader) {
         reader.AssertReadRawBytes(FullTypeIdBytes);
         return true;
      }

      public bool ReadRaw(VoxReader reader) => true;

      public void ReadFullIntoRef(VoxReader reader, ref bool val) => val = ReadFull(reader);
      public void ReadRawIntoRef(VoxReader reader, ref bool val) => val = ReadRaw(reader);
   }

   /// <summary>
   /// Handles the case of reading a false
   /// </summary>
   public class BoolFalseVoxSerializer : IVoxSerializer<bool> {
      private const int kFalseTypeId = (int)BuiltInVoxTypeIds.BoolFalse;

      public int SimpleTypeId { get; } = kFalseTypeId;
      public int[] FullTypeId { get; } = new[] { kFalseTypeId };
      public byte[] FullTypeIdBytes { get; } = kFalseTypeId.ToVariableIntBytes();
      public bool IsUpdatable => false;

      public void WriteRawObject(VoxWriter writer, object val) { }
      public object ReadRawObject(VoxReader reader) => false;

      public void WriteFull(VoxWriter writer, ref bool val) {
         val.AssertIsTrue();
         writer.WriteTypeIdBytes(FullTypeIdBytes);
      }

      public void WriteRaw(VoxWriter writer, ref bool val) { }

      public bool ReadFull(VoxReader reader) {
         reader.AssertReadRawBytes(FullTypeIdBytes);
         return false;
      }

      public bool ReadRaw(VoxReader reader) => false;

      public void ReadFullIntoRef(VoxReader reader, ref bool val) => val = ReadFull(reader);
      public void ReadRawIntoRef(VoxReader reader, ref bool val) => val = ReadRaw(reader);
   }

   public static class BoolVoxExtensions {
      public static void WriteRawBoolean(this VoxWriter writer, bool b) => BoolVoxSerializer.Instance.WriteRaw(writer, ref b);
      public static void WriteFullBoolean(this VoxWriter writer, bool b) => BoolVoxSerializer.Instance.WriteFull(writer, ref b);
      public static bool ReadRawBoolean(this VoxReader reader) => BoolVoxSerializer.Instance.ReadRaw(reader);
      public static bool ReadFullBoolean(this VoxReader reader) => BoolVoxSerializer.Instance.ReadFull(reader);
   }

   [VoxType((int)BuiltInVoxTypeIds.Int8, RedirectToType = typeof(SByte), Flags = VoxTypeFlags.StubRaw | VoxTypeFlags.NonUpdatable)]
   public static partial class VoxGeneration_Int8 {
      public static partial void Stub_WriteRaw_SByte(VoxWriter writer, sbyte value) => writer.InnerWriter.Write((sbyte)value);
      public static partial sbyte Stub_ReadRaw_SByte(VoxReader reader) => reader.InnerReader.ReadSByte();
   }

   [VoxType((int)BuiltInVoxTypeIds.Int16, RedirectToType = typeof(Int16), Flags = VoxTypeFlags.StubRaw | VoxTypeFlags.NonUpdatable)]
   public static partial class VoxGeneration_Int16 {
      public static partial void Stub_WriteRaw_Int16(VoxWriter writer, short value) => writer.InnerWriter.Write((short)value);
      public static partial short Stub_ReadRaw_Int16(VoxReader reader) => reader.InnerReader.ReadInt16();
   }

   [VoxType((int)BuiltInVoxTypeIds.Int32, RedirectToType = typeof(Int32), Flags = VoxTypeFlags.StubRaw | VoxTypeFlags.NonUpdatable)]
   public static partial class VoxGeneration_Int32 {
      public static partial void Stub_WriteRaw_Int32(VoxWriter writer, int value) => writer.InnerWriter.Write((int)value);
      public static partial int Stub_ReadRaw_Int32(VoxReader reader) => reader.InnerReader.ReadInt32();
   }

   [VoxType((int)BuiltInVoxTypeIds.Int64, RedirectToType = typeof(Int64), Flags = VoxTypeFlags.StubRaw | VoxTypeFlags.NonUpdatable)]
   public static partial class VoxGeneration_Int64 {
      public static partial void Stub_WriteRaw_Int64(VoxWriter writer, long value) => writer.InnerWriter.Write((long)value);
      public static partial long Stub_ReadRaw_Int64(VoxReader reader) => reader.InnerReader.ReadInt64();
   }

   [VoxType((int)BuiltInVoxTypeIds.UInt8, RedirectToType = typeof(Byte), Flags = VoxTypeFlags.StubRaw | VoxTypeFlags.NonUpdatable)]
   public static partial class VoxGeneration_UInt8 {
      public static partial void Stub_WriteRaw_Byte(VoxWriter writer, byte value) => writer.InnerWriter.Write((byte)value);
      public static partial byte Stub_ReadRaw_Byte(VoxReader reader) => reader.InnerReader.ReadByte();
   }

   [VoxType((int)BuiltInVoxTypeIds.UInt16, RedirectToType = typeof(UInt16), Flags = VoxTypeFlags.StubRaw | VoxTypeFlags.NonUpdatable)]
   public static partial class VoxGeneration_UInt16 {
      public static partial void Stub_WriteRaw_UInt16(VoxWriter writer, ushort value) => writer.InnerWriter.Write((ushort)value);
      public static partial ushort Stub_ReadRaw_UInt16(VoxReader reader) => reader.InnerReader.ReadUInt16();
   }

   [VoxType((int)BuiltInVoxTypeIds.UInt32, RedirectToType = typeof(UInt32), Flags = VoxTypeFlags.StubRaw | VoxTypeFlags.NonUpdatable)]
   public static partial class VoxGeneration_UInt32 {
      public static partial void Stub_WriteRaw_UInt32(VoxWriter writer, uint value) => writer.InnerWriter.Write((uint)value);
      public static partial uint Stub_ReadRaw_UInt32(VoxReader reader) => reader.InnerReader.ReadUInt32();
   }

   [VoxType((int)BuiltInVoxTypeIds.UInt64, RedirectToType = typeof(UInt64), Flags = VoxTypeFlags.StubRaw | VoxTypeFlags.NonUpdatable)]
   public static partial class VoxGeneration_UInt64 {
      public static partial void Stub_WriteRaw_UInt64(VoxWriter writer, ulong value) => writer.InnerWriter.Write((ulong)value);
      public static partial ulong Stub_ReadRaw_UInt64(VoxReader reader) => reader.InnerReader.ReadUInt64();
   }

   [VoxType((int)BuiltInVoxTypeIds.Float, RedirectToType = typeof(Single), Flags = VoxTypeFlags.StubRaw | VoxTypeFlags.NonUpdatable)]
   public static partial class VoxGeneration_Float {
      public static partial void Stub_WriteRaw_Single(VoxWriter writer, float value) => writer.InnerWriter.Write((float)value);
      public static partial float Stub_ReadRaw_Single(VoxReader reader) => reader.InnerReader.ReadSingle();
   }

   [VoxType((int)BuiltInVoxTypeIds.Double, RedirectToType = typeof(Double), Flags = VoxTypeFlags.StubRaw | VoxTypeFlags.NonUpdatable)]
   public static partial class VoxGeneration_Double {
      public static partial void Stub_WriteRaw_Double(VoxWriter writer, double value) => writer.InnerWriter.Write((double)value);
      public static partial double Stub_ReadRaw_Double(VoxReader reader) => reader.InnerReader.ReadDouble();
   }

   [VoxType((int)BuiltInVoxTypeIds.String, RedirectToType = typeof(String), Flags = VoxTypeFlags.StubRaw | VoxTypeFlags.NonUpdatable)]
   public static partial class VoxGeneration_String {
      public static partial void Stub_WriteRaw_String(VoxWriter writer, string? value) {
         if (value == null) {
            writer.InnerWriter.Write((int)-1);
         } else {
            writer.InnerWriter.WriteLongText(value);
         }
      }

      public static partial string? Stub_ReadRaw_String(VoxReader reader) {
         var len = reader.InnerReader.ReadInt32();
         if (len == -1) return null;
         return reader.InnerReader.ReadStringOfLength(len);
      }
   }

   [VoxType((int)BuiltInVoxTypeIds.Guid, RedirectToType = typeof(Guid), Flags = VoxTypeFlags.StubRaw | VoxTypeFlags.NonUpdatable)]
   public static partial class VoxGeneration_Guid {
      public static partial void Stub_WriteRaw_Guid(VoxWriter writer, Guid value) => writer.InnerWriter.Write(value);
      public static partial Guid Stub_ReadRaw_Guid(VoxReader reader) => reader.InnerReader.ReadGuid();
   }

   [VoxType((int)BuiltInVoxTypeIds.Vector2, RedirectToType = typeof(Vector2))]
   public static partial class VoxGeneration_Vector2 { }

   [VoxType((int)BuiltInVoxTypeIds.Vector3, RedirectToType = typeof(Vector3))]
   public static partial class VoxGeneration_Vector3 { }

   [VoxType((int)BuiltInVoxTypeIds.Vector4, RedirectToType = typeof(Vector4))]
   public static partial class VoxGeneration_Vector4 { }

   [VoxType((int)BuiltInVoxTypeIds.DateTime, RedirectToType = typeof(DateTime), Flags = VoxTypeFlags.StubRaw | VoxTypeFlags.NonUpdatable)]
   public static partial class VoxGeneration_DateTime {
      public static partial void Stub_WriteRaw_DateTime(VoxWriter writer, DateTime value) => writer.InnerWriter.Write((long)value.ToBinary());
      public static partial DateTime Stub_ReadRaw_DateTime(VoxReader reader) => DateTime.FromBinary(reader.InnerReader.ReadInt64());
   }

   [VoxType((int)BuiltInVoxTypeIds.DateTimeOffset, RedirectToType = typeof(DateTimeOffset), Flags = VoxTypeFlags.StubRaw | VoxTypeFlags.NonUpdatable)]
   public static partial class VoxGeneration_DateTimeOffset {
      public static partial void Stub_WriteRaw_DateTimeOffset(VoxWriter writer, DateTimeOffset value) {
         writer.WriteRawDateTime(value.DateTime);
         writer.WriteRawInt16((short)(value.Offset.Ticks / TimeSpan.TicksPerMinute)); // always a round short
      }

      public static partial DateTimeOffset Stub_ReadRaw_DateTimeOffset(VoxReader reader) {
         var dt = reader.ReadRawDateTime();
         var offset = reader.ReadRawInt16();
         return new DateTimeOffset(dt, TimeSpan.FromMinutes(offset));
      }
   }

   [VoxType((int)BuiltInVoxTypeIds.TimeSpan, RedirectToType = typeof(TimeSpan), Flags = VoxTypeFlags.StubRaw | VoxTypeFlags.NonUpdatable)]
   public static partial class VoxGeneration_TimeSpan {
      public static partial void Stub_WriteRaw_TimeSpan(VoxWriter writer, TimeSpan value) => writer.InnerWriter.Write((long)value.Ticks);
      public static partial TimeSpan Stub_ReadRaw_TimeSpan(VoxReader reader) => TimeSpan.FromTicks(reader.InnerReader.ReadInt64());
   }

   [VoxType((int)BuiltInVoxTypeIds.Type, RedirectToType = typeof(Type), Flags = VoxTypeFlags.StubRaw | VoxTypeFlags.NonUpdatable)]
   public static partial class VoxGeneration_Type {
      public static partial void Stub_WriteRaw_Type(VoxWriter writer, Type value) => writer.WriteFullType(value);
      public static partial Type Stub_ReadRaw_Type(VoxReader reader) => reader.ReadFullType();
   }

   [VoxType((int)BuiltInVoxTypeIds.ReservedForInternalVoxTest6, RedirectToType = typeof(StringSplitOptions))]
   public static partial class VoxGeneration_StringSplitOptions { }

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

   [VoxType((int)BuiltInVoxTypeIds.Null, Flags = VoxTypeFlags.NonUpdatable | VoxTypeFlags.NoCodeGen, VanityRedirectFromType = typeof(VoxTypePlaceholders.RuntimePolymorphicNull))]
   public class RuntimePolymorphicNullSerializer : IVoxSerializer<object> {
      private static class Constants {
         public const int SimpleTypeId = (int)BuiltInVoxTypeIds.Null;
         public static readonly byte[] SimpleTypeIdBytes = SimpleTypeId.ToVariableIntBytes();
      }

      public RuntimePolymorphicNullSerializer() {
         FullTypeId = new[] { Constants.SimpleTypeId };
         FullTypeIdBytes = Constants.SimpleTypeIdBytes;
      }

      public int SimpleTypeId => Constants.SimpleTypeId;
      public int[] FullTypeId { get; }
      public byte[] FullTypeIdBytes { get; }
      public bool IsUpdatable => false;

      public void WriteRawObject(VoxWriter writer, object val) {
         var x = val;
         WriteRaw(writer, ref x);
      }

      public object ReadRawObject(VoxReader reader) => ReadRaw(reader);

      public void WriteFull(VoxWriter writer, ref object val) {
         writer.WriteTypeIdBytes(FullTypeIdBytes);
         WriteRaw(writer, ref val);
      }

      public void WriteRaw(VoxWriter writer, ref object val) {
         val.AssertIsNull();
      }

      public object ReadFull(VoxReader reader) {
         reader.AssertReadTypeIdBytes(FullTypeIdBytes);
         return ReadRaw(reader);
      }

      public object ReadRaw(VoxReader reader) {
         return null;
      }

      public void ReadFullIntoRef(VoxReader reader, ref object val)
         => throw new NotSupportedException();

      public void ReadRawIntoRef(VoxReader reader, ref object val)
         => throw new NotSupportedException();
   }

   public class ThrowawaySerializerBase<T> : IVoxSerializer<T> {
      public ThrowawaySerializerBase(int tid) {
         SimpleTypeId = (int)tid;
         FullTypeId = new[] { (int)tid };
         FullTypeIdBytes = ((int)tid).ToVariableIntBytes();
      }

      public int SimpleTypeId { get; }
      public int[] FullTypeId { get; }
      public byte[] FullTypeIdBytes { get; }
      public bool IsUpdatable => false;

      public void WriteRawObject(VoxWriter writer, object val) => throw new InvalidOperationException();
      public object ReadRawObject(VoxReader reader) => throw new InvalidOperationException();
      public void WriteFull(VoxWriter writer, ref T val) => throw new InvalidOperationException();
      public void WriteRaw(VoxWriter writer, ref T val) => throw new InvalidOperationException();
      public T ReadFull(VoxReader reader) => throw new InvalidOperationException();
      public T ReadRaw(VoxReader reader) => throw new InvalidOperationException();
      public void ReadFullIntoRef(VoxReader reader, ref T val) => throw new InvalidOperationException();
      public void ReadRawIntoRef(VoxReader reader, ref T val) => throw new InvalidOperationException();
   }

   [VoxType((int)BuiltInVoxTypeIds.Void, Flags = VoxTypeFlags.NonUpdatable | VoxTypeFlags.NoCodeGen, VanityRedirectFromType = typeof(void))]
   public class VoidThrowAlwaysSerializer : ThrowawaySerializerBase<object> {
      public VoidThrowAlwaysSerializer() : base((int)BuiltInVoxTypeIds.Void) { }
   }

   [VoxType((int)BuiltInVoxTypeIds.Object, Flags = VoxTypeFlags.NonUpdatable | VoxTypeFlags.NoCodeGen, VanityRedirectFromType = typeof(object))]
   public class ObjectThrowAlwaysSerializer : ThrowawaySerializerBase<object> {
      public ObjectThrowAlwaysSerializer() : base((int)BuiltInVoxTypeIds.Object) { }
   }

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

      public void WriteFull(VoxWriter writer, ref T[]? val) {
         writer.WriteTypeIdBytes(FullTypeIdBytes);
         WriteRaw(writer, ref val);
      }

      public void WriteRaw(VoxWriter writer, ref T[]? val) {
         if (val == null) {
            writer.InnerWriter.Write((int)-1);
            return;
         }

         writer.InnerWriter.Write((int)val.Length);
         for (var i = 0; i < val.Length; i++) {
            elementSerializer.WriteFull(writer, ref val[i]);
         }
      }

      public T[]? ReadFull(VoxReader reader) {
         reader.AssertReadTypeIdBytes(FullTypeIdBytes);
         return ReadRaw(reader);
      }

      public T[]? ReadRaw(VoxReader reader) {
         var len = reader.InnerReader.ReadInt32();
         if (len == -1) return null;

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
         if (val == null) {
            writer.InnerWriter.Write((int)-1);
            return;
         }

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
         if (len == -1) return null;

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

   [VoxType((int)BuiltInVoxTypeIds.HashSet, Flags = VoxTypeFlags.NonUpdatable | VoxTypeFlags.NoCodeGen, VanityRedirectFromType = typeof(HashSet<>))]
   public class RuntimePolymorphicHashSetSerializer<T> : IVoxSerializer<HashSet<T>> {
      private static class Constants {
         public const int SimpleTypeId = (int)BuiltInVoxTypeIds.HashSet;
         public static readonly byte[] SimpleTypeIdBytes = SimpleTypeId.ToVariableIntBytes();
      }

      private readonly IVoxSerializer<T> elementSerializer;

      public RuntimePolymorphicHashSetSerializer(VoxContext vc) {
         elementSerializer = vc.SerializerContainer.GetSerializerForType<T>();
         FullTypeId = Arrays.Concat(Constants.SimpleTypeId, elementSerializer.FullTypeId);
         FullTypeIdBytes = Arrays.Concat(Constants.SimpleTypeIdBytes, elementSerializer.FullTypeIdBytes);
      }

      public int SimpleTypeId => Constants.SimpleTypeId;
      public int[] FullTypeId { get; }
      public byte[] FullTypeIdBytes { get; }
      public bool IsUpdatable => false;

      public void WriteRawObject(VoxWriter writer, object val) {
         var x = (HashSet<T>)val;
         WriteRaw(writer, ref x);
      }

      public object ReadRawObject(VoxReader reader) => ReadRaw(reader);

      public void WriteFull(VoxWriter writer, ref HashSet<T> val) {
         writer.WriteTypeIdBytes(FullTypeIdBytes);
         WriteRaw(writer, ref val);
      }

      public void WriteRaw(VoxWriter writer, ref HashSet<T> val) {
         if (val == null) {
            writer.InnerWriter.Write((int)-1);
            return;
         }

         var count = val.Count;
         writer.InnerWriter.Write((int)count);

         var numObserved = 0;
         foreach (var x in val) {
            var el = x; // unfortunate struct copy
            elementSerializer.WriteFull(writer, ref el);
            numObserved++;
         }

         numObserved.AssertEquals(count);
      }

      public HashSet<T> ReadFull(VoxReader reader) {
         reader.AssertReadTypeIdBytes(FullTypeIdBytes);
         return ReadRaw(reader);
      }

      public HashSet<T> ReadRaw(VoxReader reader) {
         var len = reader.InnerReader.ReadInt32();
         if (len == -1) return null;

         var res = new HashSet<T>(len);
         for (var i = 0; i < len; i++) {
            res.Add(elementSerializer.ReadFull(reader));
         }
         return res;
      }

      public void ReadFullIntoRef(VoxReader reader, ref HashSet<T> val)
         => throw new NotSupportedException();

      public void ReadRawIntoRef(VoxReader reader, ref HashSet<T> val)
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
         if (val == null) {
            writer.InnerWriter.Write((int)-1);
            return;
         }

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
         if (len == -1) return null;

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