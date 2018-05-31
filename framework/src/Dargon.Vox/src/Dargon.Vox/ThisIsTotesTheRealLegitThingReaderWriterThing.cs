using System;
using System.Diagnostics;
using System.IO;
using Dargon.Commons.Collections;
using Dargon.Commons.Exceptions;
using Dargon.Vox.Internals.TypePlaceholders;
using Dargon.Vox.Utilities;
using NLog;

namespace Dargon.Vox {
   public class ThisIsTotesTheRealLegitThingReaderWriterThing {
      private static readonly Logger logger = LogManager.GetCurrentClassLogger();
      private static readonly IReadOnlySet<Type> integerTypes = ImmutableSet.Of(typeof(sbyte), typeof(short), typeof(int), typeof(long), typeof(byte), typeof(ushort), typeof(uint), typeof(ulong));
      private readonly ThingReaderWriterContainer thingReaderWriterContainer;
      private readonly TypeReader typeReader;

      public ThisIsTotesTheRealLegitThingReaderWriterThing(ThingReaderWriterContainer thingReaderWriterContainer, TypeReader typeReader) {
         this.thingReaderWriterContainer = thingReaderWriterContainer;
         this.typeReader = typeReader;
      }

      public object ReadThing(VoxBinaryReader thingReader, Type hintType) {
         var type = typeReader.ReadType(thingReader);

         // special cases for type deserialization
         if (type == typeof(TypePlaceholderBoolTrue) || type == typeof(TypePlaceholderBoolFalse) || type == typeof(TypePlaceholderNull)) {
            // fall straight to reader, ignoring hintType
            return thingReaderWriterContainer.Get(type).ReadBody(thingReader);
         } else if (type == typeof(Type)) {
            Trace.Assert(hintType == null || typeof(Type).IsAssignableFrom(hintType));
            return thingReaderWriterContainer.Get(type).ReadBody(thingReader);
         } else if (integerTypes.Contains(type)) {
            var value = thingReaderWriterContainer.Get(type).ReadBody(thingReader);
            if (hintType == null) {
               return value;
            } else if (hintType.IsEnum) {
               return Convert.ChangeType(value, Enum.GetUnderlyingType(hintType));
            } else {
               return Convert.ChangeType(value, hintType);
            }
         }

         if (hintType != null) {
            var simplifiedType = TypeSimplifier.SimplifyType(hintType);

            if (type == simplifiedType) {
               type = hintType;
            } else if (!hintType.IsAssignableFrom(type)) {
               logger.Error($"Unable to convert from {type.FullName} to hinted {hintType.FullName}.");
               throw new InvalidStateException();
            }
         }
         return thingReaderWriterContainer.Get(type).ReadBody(thingReader);
      }

      public void WriteThing(VoxBinaryWriter dest, object subject) {
         var type = SubjectTypeGetterererer.GetSubjectType(subject);
         thingReaderWriterContainer.Get(type).WriteThing(dest, subject);
      }
   }
}