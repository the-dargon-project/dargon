using System;
using System.Collections;
using System.IO;
using Dargon.Commons.Exceptions;
using Dargon.Vox.Internals.TypePlaceholders;
using Dargon.Vox.Utilities;

namespace Dargon.Vox {
   public class CollectionReaderWriter : IThingReaderWriter {
      private readonly FullTypeBinaryRepresentationCache fullTypeBinaryRepresentationCache;
      private readonly ThisIsTotesTheRealLegitThingReaderWriterThing thingReaderWriterDispatcherThing;
      private readonly Type userCollectionType;
      private readonly Type simplifiedCollectionType;
      private readonly Type elementType;

      public CollectionReaderWriter(FullTypeBinaryRepresentationCache fullTypeBinaryRepresentationCache, ThisIsTotesTheRealLegitThingReaderWriterThing thingReaderWriterDispatcherThing, Type userCollectionType) {
         this.fullTypeBinaryRepresentationCache = fullTypeBinaryRepresentationCache;
         this.thingReaderWriterDispatcherThing = thingReaderWriterDispatcherThing;
         this.userCollectionType = userCollectionType;
         this.simplifiedCollectionType = TypeSimplifier.SimplifyType(userCollectionType);
         this.elementType = EnumerableUtilities.GetEnumerableElementType(simplifiedCollectionType);
      }

      public void WriteThing(SomeMemoryStreamWrapperThing dest, object subject) {
         dest.Write(fullTypeBinaryRepresentationCache.GetOrCompute(simplifiedCollectionType));

         using (dest.ReserveLength())
         using (var countReservation = dest.ReserveCount()) {
            int count = 0;
            foreach (var x in (IEnumerable)subject) {
               count++;
               thingReaderWriterDispatcherThing.WriteThing(dest, x);
            }
            countReservation.SetValue(count);
         }
      }

      public object ReadBody(VoxBinaryReader reader) {
         var dataLength = reader.ReadVariableInt();
         reader.HandleEnterInnerBuffer(dataLength);
         try {
            var elementCount = reader.ReadVariableInt();
            var elements = Array.CreateInstance(elementType, elementCount);
            for (var i = 0; i < elements.Length; i++) {
               var thing = thingReaderWriterDispatcherThing.ReadThing(reader, null);
               elements.SetValue(thing, i);
            }
            return InternalRepresentationToHintTypeConverter.ConvertCollectionToHintType(elements, userCollectionType);
         } finally {
            reader.HandleLeaveInnerBuffer();
         }
      }
   }

   public class ByteArraySliceReaderWriter : IThingReaderWriter {
      private readonly FullTypeBinaryRepresentationCache fullTypeBinaryRepresentationCache;
      private readonly IntegerLikeThingReaderWriter<byte> byteReaderWriter;

      public ByteArraySliceReaderWriter(FullTypeBinaryRepresentationCache fullTypeBinaryRepresentationCache) {
         this.fullTypeBinaryRepresentationCache = fullTypeBinaryRepresentationCache;
         this.byteReaderWriter = new IntegerLikeThingReaderWriter<byte>(fullTypeBinaryRepresentationCache);
      }

      public void WriteThing(SomeMemoryStreamWrapperThing dest, object subject) {
         var byteArraySlice = (ByteArraySlice)subject;
         dest.Write(fullTypeBinaryRepresentationCache.GetOrCompute(typeof(byte[])));

         using (dest.ReserveLength())
         using (var countReservation = dest.ReserveCount()) {
            for (var i = 0; i < byteArraySlice.Length; i++) {
               byteReaderWriter.WriteThing(dest, byteArraySlice.Buffer[i + byteArraySlice.Offset]);
            }
            countReservation.SetValue(byteArraySlice.Length);
         }
      }

      public object ReadBody(VoxBinaryReader reader) {
         throw new InvalidStateException("Attempted to deserialize byte array slice internal type instance.");
      }
   }
}