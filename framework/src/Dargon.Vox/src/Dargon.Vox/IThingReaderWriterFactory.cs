using Dargon.Commons;
using Dargon.Vox.Utilities;
using System;
using System.Collections;
using System.IO;
using Dargon.Commons.Exceptions;
using Dargon.Vox.Internals.TypePlaceholders;
using NLog;
using SCG = System.Collections.Generic;

namespace Dargon.Vox {
   public class ThingReaderWriterFactory {
      private static Logger logger = LogManager.GetCurrentClassLogger();
      private readonly FullTypeBinaryRepresentationCache fullTypeBinaryRepresentationCache;
      private ThingReaderWriterContainer thingReaderWriterContainer;
      private ThisIsTotesTheRealLegitThingReaderWriterThing thisIsTotesTheRealLegitThingReaderWriterThing;

      public ThingReaderWriterFactory(FullTypeBinaryRepresentationCache fullTypeBinaryRepresentationCache) {
         this.fullTypeBinaryRepresentationCache = fullTypeBinaryRepresentationCache;
      }

      public void SetContainer(ThingReaderWriterContainer thingReaderWriterContainer) {
         this.thingReaderWriterContainer = thingReaderWriterContainer;
      }

      public void SetThisIsTotesTheRealLegitThingReaderWriterThing(ThisIsTotesTheRealLegitThingReaderWriterThing thisIsTotesTheRealLegitThingReaderWriterThing) {
         this.thisIsTotesTheRealLegitThingReaderWriterThing = thisIsTotesTheRealLegitThingReaderWriterThing;
      }

      public IThingReaderWriter Create(Type arg) {
         if (arg.IsArray || typeof(IEnumerable).IsAssignableFrom(arg)) {
            return new CollectionReaderWriter(fullTypeBinaryRepresentationCache, thisIsTotesTheRealLegitThingReaderWriterThing, arg);
         } else if (arg.IsGenericType && arg.GetGenericTypeDefinition() == typeof(SCG.KeyValuePair<,>)) {
            return new KeyValuePairReaderWriter(fullTypeBinaryRepresentationCache, thisIsTotesTheRealLegitThingReaderWriterThing, arg);
         } else if (arg.IsEnum) {
            var backingType = Enum.GetUnderlyingType(arg);
            var backingTypeReaderWriter = Activator.CreateInstance(typeof(IntegerLikeThingReaderWriter<>).MakeGenericType(backingType), fullTypeBinaryRepresentationCache);
            var enumThingReaderWriterType = typeof(EnumThingReaderWriter<,>).MakeGenericType(arg, backingType);
            var enumThingReaderWriter = Activator.CreateInstance(enumThingReaderWriterType, backingTypeReaderWriter);
            return (IThingReaderWriter)enumThingReaderWriter;
         } else {
            ITypeSerializer serializer;
            if (arg.GetAttributeOrNull<AutoSerializableAttribute>() != null) {
               serializer = AutoTypeSerializerFactory.Create(arg);
            } else if (typeof(ISerializableType).IsAssignableFrom(arg)) {
               serializer = (ITypeSerializer)Activator.CreateInstance(typeof(SerializableTypeTypeSerializerProxy<>).MakeGenericType(arg));
            } else {
               logger.Error("Unable to serialize type " + arg.FullName);
               throw new NotImplementedException();
            }
            var readerWriterType = typeof(UserObjectReaderWriter<>).MakeGenericType(arg);
            return (IThingReaderWriter)Activator.CreateInstance(
               readerWriterType,
               fullTypeBinaryRepresentationCache,
               thisIsTotesTheRealLegitThingReaderWriterThing,
               serializer);
         }
      }
   }

   public class WriterThing : IBodyWriter {
      private readonly ThisIsTotesTheRealLegitThingReaderWriterThing thisIsTotesTheRealLegitThingReaderWriterThing;
      private readonly SomeMemoryStreamWrapperThing dest;
      private int slotCount = 0;

      public WriterThing(ThisIsTotesTheRealLegitThingReaderWriterThing thisIsTotesTheRealLegitThingReaderWriterThing, SomeMemoryStreamWrapperThing dest) {
         this.thisIsTotesTheRealLegitThingReaderWriterThing = thisIsTotesTheRealLegitThingReaderWriterThing;
         this.dest = dest;
      }

      public int SlotCount => slotCount;

      public void Write<T>(T val) {
         slotCount++;
         thisIsTotesTheRealLegitThingReaderWriterThing.WriteThing(dest, val);
      }

      public void Write(byte[] buffer, int offset, int length) {
         slotCount++;
         thisIsTotesTheRealLegitThingReaderWriterThing.WriteThing(
            dest, 
            new ByteArraySlice(buffer, offset, length));
      }
   }

   public class ReaderThing : IBodyReader {
      private readonly ThisIsTotesTheRealLegitThingReaderWriterThing thisIsTotesTheRealLegitThingReaderWriterThing;
      private readonly VoxBinaryReader reader;
      private int slotCount;

      public ReaderThing(ThisIsTotesTheRealLegitThingReaderWriterThing thisIsTotesTheRealLegitThingReaderWriterThing, VoxBinaryReader reader, int slotCount) {
         this.thisIsTotesTheRealLegitThingReaderWriterThing = thisIsTotesTheRealLegitThingReaderWriterThing;
         this.reader = reader;
         this.slotCount = slotCount;
      }

      public int SlotCount => slotCount;

      public T Read<T>() {
         slotCount--;
         if (slotCount < 0) {
            throw new InvalidStateException("Attempted to read past serialized slot count.");
         }
         return (T)thisIsTotesTheRealLegitThingReaderWriterThing.ReadThing(reader, typeof(T));
      }
   }

   public class UserObjectReaderWriter<TUserType> : IThingReaderWriter {
      private static readonly Logger logger = LogManager.GetCurrentClassLogger();

      private readonly FullTypeBinaryRepresentationCache fullTypeBinaryRepresentationCache;
      private readonly ThisIsTotesTheRealLegitThingReaderWriterThing thisIsTotesTheRealLegitThingReaderWriterThing;
      private readonly Type simplifiedType;
      private readonly ITypeSerializer<TUserType> userTypeSerializer;

      public UserObjectReaderWriter(FullTypeBinaryRepresentationCache fullTypeBinaryRepresentationCache, ThisIsTotesTheRealLegitThingReaderWriterThing thisIsTotesTheRealLegitThingReaderWriterThing, ITypeSerializer<TUserType> userTypeSerializer) {
         this.fullTypeBinaryRepresentationCache = fullTypeBinaryRepresentationCache;
         this.thisIsTotesTheRealLegitThingReaderWriterThing = thisIsTotesTheRealLegitThingReaderWriterThing;
         this.simplifiedType = TypeSimplifier.SimplifyType(typeof(TUserType));
         this.userTypeSerializer = userTypeSerializer;
      }

      public void WriteThing(SomeMemoryStreamWrapperThing dest, object subject) {
         dest.Write(fullTypeBinaryRepresentationCache.GetOrCompute(simplifiedType));

         using (dest.ReserveLength())
         using (var slotCountReservation = dest.ReserveCount()) {
            var writer = new WriterThing(thisIsTotesTheRealLegitThingReaderWriterThing, dest);
            userTypeSerializer.Serialize(writer, (TUserType)subject);
            slotCountReservation.SetValue(writer.SlotCount);
         }
      }

      public object ReadBody(VoxBinaryReader reader) {
         var length = reader.ReadVariableInt();
         reader.HandleEnterInnerBuffer(length);
         try {
            var slotCount = reader.ReadVariableInt();
            var readerThing = new ReaderThing(thisIsTotesTheRealLegitThingReaderWriterThing, reader, slotCount);
            try {
               var subject = Activator.CreateInstance(typeof(TUserType));
               userTypeSerializer.Deserialize(readerThing, (TUserType)subject);
               return subject;
            } catch (Exception e) {
               logger.Error($"Failed to instantiate or deserialize instance of type {typeof(TUserType).Name}.");
               throw;
            }
         } finally {
            reader.HandleLeaveInnerBuffer();
         }
      }
   }
}