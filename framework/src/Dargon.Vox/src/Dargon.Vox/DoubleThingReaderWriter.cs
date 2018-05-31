using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Dargon.Vox {
   public unsafe class DoubleThingReaderWriter : IThingReaderWriter {
      private readonly FullTypeBinaryRepresentationCache fullTypeBinaryRepresentationCache;
      [ThreadStatic] private static byte[] writerBuffers;

      public DoubleThingReaderWriter(FullTypeBinaryRepresentationCache fullTypeBinaryRepresentationCache) {
         this.fullTypeBinaryRepresentationCache = fullTypeBinaryRepresentationCache;
      }

      public byte[] GetWriterBuffer() {
         var result = writerBuffers;
         if (result == null) {
            return writerBuffers = new byte[8];
         }
         return result;
      }

      public void WriteThing(VoxBinaryWriter dest, object subject) {
         dest.Write(fullTypeBinaryRepresentationCache.GetOrCompute(typeof(double)));

         var buffer = GetWriterBuffer();
         fixed (byte* pBuffer = buffer) {
            *(double*)pBuffer = (double)subject;
         }
         dest.Write(buffer);
      }

      public object ReadBody(VoxBinaryReader reader) {
         return *(double*)reader.TakeBytes(8);
      }
   }

   public class GuidThingReaderWriter : IThingReaderWriter {
      private readonly FullTypeBinaryRepresentationCache fullTypeBinaryRepresentationCache;
      [ThreadStatic] private static byte[] guidBuffer;

      public GuidThingReaderWriter(FullTypeBinaryRepresentationCache fullTypeBinaryRepresentationCache) {
         this.fullTypeBinaryRepresentationCache = fullTypeBinaryRepresentationCache;
      }

      private static byte[] GetGuidBuffer() {
         return guidBuffer ?? (guidBuffer = new byte[16]);
      }

      public void WriteThing(VoxBinaryWriter dest, object subject) {
         dest.Write(fullTypeBinaryRepresentationCache.GetOrCompute(typeof(Guid)));
         dest.Write(((Guid)subject).ToByteArray());
      }

      public unsafe object ReadBody(VoxBinaryReader reader) {
         fixed (byte* pGuidBuffer = GetGuidBuffer()) {
            reader.ReadBytes(16, pGuidBuffer);
         }
         return new Guid(guidBuffer);
      }
   }

   public unsafe class DateTimeThingReaderWriter : IThingReaderWriter {
      private readonly FullTypeBinaryRepresentationCache fullTypeBinaryRepresentationCache;
      [ThreadStatic]
      private static byte[] writerBuffer;

      public DateTimeThingReaderWriter(FullTypeBinaryRepresentationCache fullTypeBinaryRepresentationCache) {
         this.fullTypeBinaryRepresentationCache = fullTypeBinaryRepresentationCache;
      }


      public byte[] GetWriterBuffer() {
         var result = writerBuffer;
         if (result == null) {
            return writerBuffer = new byte[8];
         }
         return result;
      }

      public void WriteThing(VoxBinaryWriter dest, object subject) {
         dest.Write(fullTypeBinaryRepresentationCache.GetOrCompute(typeof(DateTime)));

         var buffer = GetWriterBuffer();
         fixed (byte* pBuffer = buffer)
         {
            *(long*)pBuffer = ((DateTime)subject).Ticks;
         }
         dest.Write(buffer);
      }

      public object ReadBody(VoxBinaryReader reader) {
         return new DateTime(*(long*)reader.TakeBytes(8));
      }
   }

   public unsafe class TimeSpanThingReaderWriter : IThingReaderWriter {
      private readonly FullTypeBinaryRepresentationCache fullTypeBinaryRepresentationCache;
      [ThreadStatic] private static byte[] writerBuffer;

      public TimeSpanThingReaderWriter(FullTypeBinaryRepresentationCache fullTypeBinaryRepresentationCache) {
         this.fullTypeBinaryRepresentationCache = fullTypeBinaryRepresentationCache;
      }


      public byte[] GetWriterBuffer() {
         var result = writerBuffer;
         if (result == null) {
            return writerBuffer = new byte[8];
         }
         return result;
      }

      public void WriteThing(VoxBinaryWriter dest, object subject) {
         dest.Write(fullTypeBinaryRepresentationCache.GetOrCompute(typeof(TimeSpan)));

         var buffer = GetWriterBuffer();
         fixed (byte* pBuffer = buffer) {
            *(long*)pBuffer = ((TimeSpan)subject).Ticks;
         }
         dest.Write(buffer);
      }

      public object ReadBody(VoxBinaryReader reader) {
         return new TimeSpan(*(long*)reader.TakeBytes(8));
      }
   }
}