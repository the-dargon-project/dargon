using System;
using System.Diagnostics;
using Dargon.Commons;
using Dargon.Vox.Utilities;

namespace Dargon.Vox {
   public class ValueTupleReaderWriter : IThingReaderWriter {
      private readonly FullTypeBinaryRepresentationCache fullTypeBinaryRepresentationCache;
      private readonly ThisIsTotesTheRealLegitThingReaderWriterThing thisIsTotesTheRealLegitThingReaderWriterThing;
      private readonly Type userTupleType;
      private readonly Type[] userGenericArgumentTypes;
      private readonly Type simplifiedTupleType;

      public ValueTupleReaderWriter(FullTypeBinaryRepresentationCache fullTypeBinaryRepresentationCache, ThisIsTotesTheRealLegitThingReaderWriterThing thisIsTotesTheRealLegitThingReaderWriterThing, Type userTupleType) {
         this.fullTypeBinaryRepresentationCache = fullTypeBinaryRepresentationCache;
         this.thisIsTotesTheRealLegitThingReaderWriterThing = thisIsTotesTheRealLegitThingReaderWriterThing;

         Trace.Assert(userTupleType.FullName.StartsWith("System.ValueTuple"));
         this.userTupleType = userTupleType;
         this.userGenericArgumentTypes = userTupleType.IsGenericType ? userTupleType.GetGenericArguments() : new Type[0];
         this.simplifiedTupleType = TypeSimplifier.SimplifyType(userTupleType);
      }

      public void WriteThing(VoxBinaryWriter dest, object subject) {
         dest.Write(fullTypeBinaryRepresentationCache.GetOrCompute(simplifiedTupleType));

         // TODO: Consider using ITuple interface
         using (dest.ReserveLength()) {
            dynamic s = subject;
            if (userGenericArgumentTypes.Length >= 1) thisIsTotesTheRealLegitThingReaderWriterThing.WriteThing(dest, s.Item1);
            if (userGenericArgumentTypes.Length >= 2) thisIsTotesTheRealLegitThingReaderWriterThing.WriteThing(dest, s.Item2);
            if (userGenericArgumentTypes.Length >= 3) thisIsTotesTheRealLegitThingReaderWriterThing.WriteThing(dest, s.Item3);
            if (userGenericArgumentTypes.Length >= 4) thisIsTotesTheRealLegitThingReaderWriterThing.WriteThing(dest, s.Item4);
            if (userGenericArgumentTypes.Length >= 5) thisIsTotesTheRealLegitThingReaderWriterThing.WriteThing(dest, s.Item5);
            if (userGenericArgumentTypes.Length >= 6) thisIsTotesTheRealLegitThingReaderWriterThing.WriteThing(dest, s.Item6);
            if (userGenericArgumentTypes.Length >= 7) thisIsTotesTheRealLegitThingReaderWriterThing.WriteThing(dest, s.Item7);
            if (userGenericArgumentTypes.Length >= 8) thisIsTotesTheRealLegitThingReaderWriterThing.WriteThing(dest, s.Rest);
         }
      }

      public object ReadBody(VoxBinaryReader reader) {
         var length = reader.ReadVariableInt();
         reader.HandleEnterInnerBuffer(length);
         try {
            var items = userGenericArgumentTypes.Map(i => thisIsTotesTheRealLegitThingReaderWriterThing.ReadThing(reader, i));
            return Activator.CreateInstance(userTupleType, items);
         } finally {
            reader.HandleLeaveInnerBuffer();
         }
      }
   }
}