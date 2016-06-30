﻿using System;
using System.IO;

namespace Dargon.Vox {
   public class EnumThingReaderWriter<TEnum, TBackingType> : IThingReaderWriter {
      private readonly IntegerLikeThingReaderWriter<TBackingType> backingTypeThingReaderWriter;

      public EnumThingReaderWriter(IntegerLikeThingReaderWriter<TBackingType> backingTypeThingReaderWriter) {
         this.backingTypeThingReaderWriter = backingTypeThingReaderWriter;
      }

      public void WriteThing(SomeMemoryStreamWrapperThing dest, object subject) {
         backingTypeThingReaderWriter.WriteThing(dest, (TBackingType)subject);
      }

      public object ReadBody(BinaryReader reader) {
         return (TEnum)Convert.ChangeType(backingTypeThingReaderWriter.ReadBody(reader), typeof(TBackingType));
      }
   }
}