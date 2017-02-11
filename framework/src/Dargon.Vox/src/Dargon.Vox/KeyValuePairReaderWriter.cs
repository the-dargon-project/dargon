using System;
using System.Diagnostics;
using System.IO;
using Dargon.Vox.Utilities;

namespace Dargon.Vox {
   public class KeyValuePairReaderWriter : IThingReaderWriter {
      private readonly FullTypeBinaryRepresentationCache fullTypeBinaryRepresentationCache;
      private readonly ThisIsTotesTheRealLegitThingReaderWriterThing thisIsTotesTheRealLegitThingReaderWriterThing;
      private readonly Type userKvpType;
      private readonly Type userKeyType;
      private readonly Type userValueType;
      private readonly Type simplifiedKvpType;

      public KeyValuePairReaderWriter(FullTypeBinaryRepresentationCache fullTypeBinaryRepresentationCache, ThisIsTotesTheRealLegitThingReaderWriterThing thisIsTotesTheRealLegitThingReaderWriterThing, Type userKvpType) {
         this.fullTypeBinaryRepresentationCache = fullTypeBinaryRepresentationCache;
         this.thisIsTotesTheRealLegitThingReaderWriterThing = thisIsTotesTheRealLegitThingReaderWriterThing;

         Trace.Assert(userKvpType.IsGenericType && userKvpType.GetGenericTypeDefinition() == typeof(System.Collections.Generic.KeyValuePair<,>));
         this.userKvpType = userKvpType;
         var genericArguments = userKvpType.GetGenericArguments();
         this.userKeyType = genericArguments[0];
         this.userValueType = genericArguments[1];
         this.simplifiedKvpType = TypeSimplifier.SimplifyType(userKvpType);
      }

      public void WriteThing(SomeMemoryStreamWrapperThing dest, object subject) {
         dest.Write(fullTypeBinaryRepresentationCache.GetOrCompute(simplifiedKvpType));

         using (dest.ReserveLength()) {
            dynamic s = subject;
            thisIsTotesTheRealLegitThingReaderWriterThing.WriteThing(dest, s.Key);
            thisIsTotesTheRealLegitThingReaderWriterThing.WriteThing(dest, s.Value);
         }
      }

      public object ReadBody(VoxBinaryReader reader) {
         var length = reader.ReadVariableInt();
         reader.HandleEnterInnerBuffer(length);
         try {
            var key = thisIsTotesTheRealLegitThingReaderWriterThing.ReadThing(reader, userKeyType);
            var value = thisIsTotesTheRealLegitThingReaderWriterThing.ReadThing(reader, userValueType);
            return Activator.CreateInstance(userKvpType, key, value);
         } finally {
            reader.HandleLeaveInnerBuffer();
         }
      }
   }
}