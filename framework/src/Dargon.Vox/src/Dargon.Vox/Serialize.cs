using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Dargon.Commons.Collections;
using Dargon.Vox.Internals.TypePlaceholders;

namespace Dargon.Vox {
   public class VoxFactory {
      public VoxSerializer Create() {
         var typeRegistry = new TypeRegistry();
         typeRegistry.RegisterReservedTypeId(typeof(sbyte), (int)TypeId.Int8);
         typeRegistry.RegisterReservedTypeId(typeof(short), (int)TypeId.Int16);
         typeRegistry.RegisterReservedTypeId(typeof(int), (int)TypeId.Int32);
         typeRegistry.RegisterReservedTypeId(typeof(long), (int)TypeId.Int64);
         typeRegistry.RegisterReservedTypeId(typeof(byte), (int)TypeId.UInt8);
         typeRegistry.RegisterReservedTypeId(typeof(ushort), (int)TypeId.UInt16);
         typeRegistry.RegisterReservedTypeId(typeof(uint), (int)TypeId.UInt32);
         typeRegistry.RegisterReservedTypeId(typeof(ulong), (int)TypeId.UInt64);

         typeRegistry.RegisterReservedTypeId(typeof(Guid), (int)TypeId.Guid);
         typeRegistry.RegisterReservedTypeId(typeof(string), (int)TypeId.String);
         typeRegistry.RegisterReservedTypeId(typeof(bool), (int)TypeId.Bool);
         typeRegistry.RegisterReservedTypeId(typeof(TypePlaceholderBoolTrue), (int)TypeId.BoolTrue);
         typeRegistry.RegisterReservedTypeId(typeof(TypePlaceholderBoolFalse), (int)TypeId.BoolFalse);
         typeRegistry.RegisterReservedTypeId(typeof(Type), (int)TypeId.Type);
         typeRegistry.RegisterReservedTypeId(typeof(float), (int)TypeId.Float);
         typeRegistry.RegisterReservedTypeId(typeof(double), (int)TypeId.Double);
         typeRegistry.RegisterReservedTypeId(typeof(DateTime), (int)TypeId.DateTime);
         typeRegistry.RegisterReservedTypeId(typeof(TimeSpan), (int)TypeId.TimeSpan);
         typeRegistry.RegisterReservedTypeId(typeof(TypePlaceholderNull), (int)TypeId.Null);
         typeRegistry.RegisterReservedTypeId(typeof(void), (int)TypeId.Void);
         typeRegistry.RegisterReservedTypeId(typeof(object), (int)TypeId.Object, () => new object());
         typeRegistry.RegisterReservedTypeId(typeof(Array), (int)TypeId.Array);
         typeRegistry.RegisterReservedTypeId(typeof(Dictionary<,>), (int)TypeId.Map);
         typeRegistry.RegisterReservedTypeId(typeof(KeyValuePair<,>), (int)TypeId.KeyValuePair);
         typeRegistry.RegisterReservedTypeId(typeof(ValueTuple), (int)TypeId.ValueTuple0);
         typeRegistry.RegisterReservedTypeId(typeof(ValueTuple<>), (int)TypeId.ValueTuple1);
         typeRegistry.RegisterReservedTypeId(typeof(ValueTuple<,>), (int)TypeId.ValueTuple2);
         typeRegistry.RegisterReservedTypeId(typeof(ValueTuple<,,>), (int)TypeId.ValueTuple3);
         typeRegistry.RegisterReservedTypeId(typeof(ValueTuple<,,,>), (int)TypeId.ValueTuple4);
         typeRegistry.RegisterReservedTypeId(typeof(ValueTuple<,,,,>), (int)TypeId.ValueTuple5);
         typeRegistry.RegisterReservedTypeId(typeof(ValueTuple<,,,,,>), (int)TypeId.ValueTuple6);
         typeRegistry.RegisterReservedTypeId(typeof(ValueTuple<,,,,,,>), (int)TypeId.ValueTuple7);
         typeRegistry.RegisterReservedTypeId(typeof(ValueTuple<,,,,,,,>), (int)TypeId.ValueTuple8);

         var fullTypeBinaryRepresentationCache = new FullTypeBinaryRepresentationCache(typeRegistry);
         var typeReader = new TypeReader(typeRegistry);
         var thingReaderWriterFactory = new ThingReaderWriterFactory(fullTypeBinaryRepresentationCache);
         var thingReaderWriterContainer = new ThingReaderWriterContainer(thingReaderWriterFactory);
         thingReaderWriterContainer.AddOrThrow(typeof(string), new StringThingReaderWriter(fullTypeBinaryRepresentationCache));
         thingReaderWriterContainer.AddOrThrow(typeof(sbyte), new IntegerLikeThingReaderWriter<sbyte>(fullTypeBinaryRepresentationCache));
         thingReaderWriterContainer.AddOrThrow(typeof(short), new IntegerLikeThingReaderWriter<short>(fullTypeBinaryRepresentationCache));
         thingReaderWriterContainer.AddOrThrow(typeof(int), new IntegerLikeThingReaderWriter<int>(fullTypeBinaryRepresentationCache));
         thingReaderWriterContainer.AddOrThrow(typeof(long), new IntegerLikeThingReaderWriter<long>(fullTypeBinaryRepresentationCache));
         thingReaderWriterContainer.AddOrThrow(typeof(byte), new IntegerLikeThingReaderWriter<byte>(fullTypeBinaryRepresentationCache));
         thingReaderWriterContainer.AddOrThrow(typeof(ushort), new IntegerLikeThingReaderWriter<ushort>(fullTypeBinaryRepresentationCache));
         thingReaderWriterContainer.AddOrThrow(typeof(uint), new IntegerLikeThingReaderWriter<uint>(fullTypeBinaryRepresentationCache));
         thingReaderWriterContainer.AddOrThrow(typeof(ulong), new IntegerLikeThingReaderWriter<ulong>(fullTypeBinaryRepresentationCache));
         thingReaderWriterContainer.AddOrThrow(typeof(Guid), new GuidThingReaderWriter(fullTypeBinaryRepresentationCache));
         thingReaderWriterContainer.AddOrThrow(typeof(bool), new BoolThingReaderWriter());
         thingReaderWriterContainer.AddOrThrow(typeof(TypePlaceholderBoolTrue), new BoolTrueThingReaderWriter(fullTypeBinaryRepresentationCache));
         thingReaderWriterContainer.AddOrThrow(typeof(TypePlaceholderBoolFalse), new BoolFalseThingReaderWriter(fullTypeBinaryRepresentationCache));
         thingReaderWriterContainer.AddOrThrow(typeof(Type), new TypeThingReaderWriter(fullTypeBinaryRepresentationCache, typeReader));
         thingReaderWriterContainer.AddOrThrow(typeof(float), new FloatThingReaderWriter(fullTypeBinaryRepresentationCache));
         thingReaderWriterContainer.AddOrThrow(typeof(double), new DoubleThingReaderWriter(fullTypeBinaryRepresentationCache));
         thingReaderWriterContainer.AddOrThrow(typeof(DateTime), new DateTimeThingReaderWriter(fullTypeBinaryRepresentationCache));
         thingReaderWriterContainer.AddOrThrow(typeof(TimeSpan), new TimeSpanThingReaderWriter(fullTypeBinaryRepresentationCache));
         thingReaderWriterContainer.AddOrThrow(typeof(TypePlaceholderNull), new NullThingReaderWriter(fullTypeBinaryRepresentationCache));
         thingReaderWriterContainer.AddOrThrow(typeof(void), new VoidThingReaderWriter());
         thingReaderWriterContainer.AddOrThrow(typeof(object), new SystemObjectThingReaderWriter(fullTypeBinaryRepresentationCache));

         // internal magic types
         thingReaderWriterContainer.AddOrThrow(typeof(ByteArraySlice), new ByteArraySliceReaderWriter(fullTypeBinaryRepresentationCache));

         var thisIsTotesTheRealLegitThingReaderWriterThing = new ThisIsTotesTheRealLegitThingReaderWriterThing(thingReaderWriterContainer, typeReader);
         var frameReader = new FrameReader(thisIsTotesTheRealLegitThingReaderWriterThing);
         var frameWriter = new FrameWriter(thisIsTotesTheRealLegitThingReaderWriterThing);
         thingReaderWriterFactory.SetThisIsTotesTheRealLegitThingReaderWriterThing(thisIsTotesTheRealLegitThingReaderWriterThing);

         return new VoxSerializer(typeRegistry, frameReader, frameWriter);
      }
   }

   public class ThingReaderWriterContainer {
      private readonly CopyOnAddDictionary<Type, IThingReaderWriter> storage = new CopyOnAddDictionary<Type, IThingReaderWriter>();
      private readonly Func<Type, IThingReaderWriter> createThingReaderWriter;

      public ThingReaderWriterContainer(ThingReaderWriterFactory thingReaderWriterFactory) {
         createThingReaderWriter = thingReaderWriterFactory.Create;
      }

      public void AddOrThrow(Type type, IThingReaderWriter thingReaderWriter) {
         storage.AddOrThrow(type, thingReaderWriter);
      }

      public IThingReaderWriter Get(Type type) {
         return storage.GetOrAdd(type, createThingReaderWriter);
      }
   }

   public class VoxSerializer {
      private readonly TypeRegistry typeRegistry;
      private readonly FrameReader frameReader;
      private readonly FrameWriter frameWriter;

      public VoxSerializer(TypeRegistry typeRegistry, FrameReader frameReader, FrameWriter frameWriter) {
         this.typeRegistry = typeRegistry;
         this.frameReader = frameReader;
         this.frameWriter = frameWriter;
      }

      public void ImportTypes(VoxTypes voxTypes) {
         typeRegistry.ImportTypes(voxTypes);
      }

      public void Serialize(Stream dest, object subject) {
         var scratch = new MemoryStream();
         var target = new VoxBinaryWriter(scratch);
         frameWriter.WriteFrame(target, subject);
         target.CopyTo(dest);
      }

      public object Deserialize(Stream dest, Type hintType = null) {
         return frameReader.ReadFrame(dest, hintType);
      }

      public T Clone<T>(T subject) {
         var ms = new MemoryStream();
         Serialize(ms, subject);
         var serializationLength = ms.Position;
         ms.Position = 0;
         var result = (T)Deserialize(ms, subject?.GetType());
         Trace.Assert(ms.Position == serializationLength);
         return result;
      }
   }

   public static class Globals {
      static Globals() {
         Serializer = new VoxFactory().Create();
      }

      public static VoxSerializer Serializer { get; }
   }

   public static class Serialize {
      public static void To(Stream dest, object subject) {
         Globals.Serializer.Serialize(dest, subject);
      }
   }

   public static class Deserialize {
      public static T From<T>(Stream dest) {
         return (T)From(dest, typeof(T));
      }

      public static object From(Stream dest, Type hintType = null) {
         return Globals.Serializer.Deserialize(dest, hintType);
      }
   }

   public static class CloneStatics {
      [ThreadStatic] private static MemoryStream ms;

      private static MemoryStream GetMemoryStream() {
         return ms ?? (ms = new MemoryStream());
      }

      public static T DeepCloneSerializable<T>(this T subject) {
         var ms = GetMemoryStream();
         Serialize.To(ms, subject);
         ms.Position = 0;
         var result = Deserialize.From<T>(ms);
         ms.SetLength(0);
         return result;
      }
   }
}