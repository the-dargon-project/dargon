﻿using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Threading;
using Dargon.Commons.Exceptions;
using Dargon.Commons.FormatProviders;
using Dargon.Vox.Internals.TypePlaceholders;
using Dargon.Vox.Utilities;

namespace Dargon.Vox {
   public class TypeThingReaderWriter : IThingReaderWriter {
      private readonly FullTypeBinaryRepresentationCache fullTypeBinaryRepresentationCache;
      private readonly TypeReader typeReader;

      public TypeThingReaderWriter(FullTypeBinaryRepresentationCache fullTypeBinaryRepresentationCache, TypeReader typeReader) {
         this.fullTypeBinaryRepresentationCache = fullTypeBinaryRepresentationCache;
         this.typeReader = typeReader;
      }

      public void WriteThing(SomeMemoryStreamWrapperThing dest, object subject) {
         var type = TypeSimplifier.SimplifyType((Type)subject);
         var typeBinaryRepresentation = fullTypeBinaryRepresentationCache.GetOrCompute(type);
         dest.Write(fullTypeBinaryRepresentationCache.GetOrCompute(typeof(Type)));
         dest.Write(typeBinaryRepresentation);
      }

      public object ReadBody(BinaryReader reader) {
         return typeReader.ReadType(reader.ReadByte);
      }
   }

   public class StringThingReaderWriter : IThingReaderWriter {
      private readonly FullTypeBinaryRepresentationCache fullTypeBinaryRepresentationCache;

      public StringThingReaderWriter(FullTypeBinaryRepresentationCache fullTypeBinaryRepresentationCache) {
         this.fullTypeBinaryRepresentationCache = fullTypeBinaryRepresentationCache;
      }

      public void WriteThing(SomeMemoryStreamWrapperThing dest, object subject) {
         dest.Write(fullTypeBinaryRepresentationCache.GetOrCompute(typeof(string)));

         var str = (string)subject;
         using(dest.ReserveLength()) {
            var bytes = Encoding.UTF8.GetBytes(str);
            dest.Write(bytes);
         }
      }

      public object ReadBody(BinaryReader reader) {
         var length = VarIntSerializer.ReadVariableInt(reader.ReadByte);
         var bytes = reader.ReadBytes(length);
         return Encoding.UTF8.GetString(bytes);
      }
   }

   public class BoolThingReaderWriter : IThingReaderWriter {
      public void WriteThing(SomeMemoryStreamWrapperThing dest, object subject) {
         throw new InvalidStateException($"WriteThing invoked on {nameof(BoolThingReaderWriter)}");
      }

      public object ReadBody(BinaryReader reader) {
         throw new InvalidStateException($"ReadBody invoked on {nameof(BoolThingReaderWriter)}");
      }
   }

   public class BoolTrueThingReaderWriter : IThingReaderWriter {
      private readonly FullTypeBinaryRepresentationCache fullTypeBinaryRepresentationCache;

      public BoolTrueThingReaderWriter(FullTypeBinaryRepresentationCache fullTypeBinaryRepresentationCache) {
         this.fullTypeBinaryRepresentationCache = fullTypeBinaryRepresentationCache;
      }

      public void WriteThing(SomeMemoryStreamWrapperThing dest, object subject) {
         dest.Write(fullTypeBinaryRepresentationCache.GetOrCompute(typeof(TBoolTrue)));
      }

      public object ReadBody(BinaryReader reader) {
         return true;
      }
   }

   public class BoolFalseThingReaderWriter : IThingReaderWriter {
      private readonly FullTypeBinaryRepresentationCache fullTypeBinaryRepresentationCache;

      public BoolFalseThingReaderWriter(FullTypeBinaryRepresentationCache fullTypeBinaryRepresentationCache) {
         this.fullTypeBinaryRepresentationCache = fullTypeBinaryRepresentationCache;
      }

      public void WriteThing(SomeMemoryStreamWrapperThing dest, object subject) {
         dest.Write(fullTypeBinaryRepresentationCache.GetOrCompute(typeof(TBoolFalse)));
      }

      public object ReadBody(BinaryReader reader) {
         return false;
      }
   }

   public unsafe class FloatThingReaderWriter : IThingReaderWriter {
      private readonly FullTypeBinaryRepresentationCache fullTypeBinaryRepresentationCache;
      [ThreadStatic]
      private static byte[] writerBuffers;

      public FloatThingReaderWriter(FullTypeBinaryRepresentationCache fullTypeBinaryRepresentationCache) {
         this.fullTypeBinaryRepresentationCache = fullTypeBinaryRepresentationCache;
      }

      public byte[] GetWriterBuffer() {
         var result = writerBuffers;
         if (result == null) {
            return writerBuffers = new byte[4];
         }
         return result;
      }

      public void WriteThing(SomeMemoryStreamWrapperThing dest, object subject) {
         dest.Write(fullTypeBinaryRepresentationCache.GetOrCompute(typeof(float)));

         var buffer = GetWriterBuffer();
         fixed (byte* pBuffer = buffer)
         {
            *(float*)pBuffer = (float)subject;
         }
         dest.Write(buffer);
      }

      public object ReadBody(BinaryReader reader) {
         var pBuffer = stackalloc byte[4];
         pBuffer[0] = reader.ReadByte();
         pBuffer[1] = reader.ReadByte();
         pBuffer[2] = reader.ReadByte();
         pBuffer[3] = reader.ReadByte();
         return *(float*)pBuffer;
      }
   }

   public unsafe class IntegerLikeThingReaderWriter<T> : IThingReaderWriter {
      private readonly FullTypeBinaryRepresentationCache fullTypeBinaryRepresentationCache;
      private readonly Action<SomeMemoryStreamWrapperThing, object> writeThingImpl;
      private readonly Func<BinaryReader, object> readThingImpl;
      [ThreadStatic] private static byte[] writerBuffer;

      public IntegerLikeThingReaderWriter(FullTypeBinaryRepresentationCache fullTypeBinaryRepresentationCache) {
         this.fullTypeBinaryRepresentationCache = fullTypeBinaryRepresentationCache;
         switch (typeof(T).Name) {
            case nameof(SByte):
               writeThingImpl = (ms, subject) => HandleWriteThingInt8(ms, (sbyte)subject);
               readThingImpl = HandleReadBodyInt8;
               break;
            case nameof(Int16):
               writeThingImpl = (ms, subject) => HandleWriteThingInt16(ms, (short)subject);
               readThingImpl = HandleReadBodyInt16;
               break;
            case nameof(Int32):
               writeThingImpl = (ms, subject) => HandleWriteThingInt32(ms, (int)subject);
               readThingImpl = HandleReadBodyInt32;
               break;
            case nameof(Int64):
               writeThingImpl = (ms, subject) => HandleWriteThingInt64(ms, (long)subject);
               readThingImpl = HandleReadBodyInt64;
               break;
            case nameof(Byte):
               writeThingImpl = (ms, subject) => HandleWriteThingUInt8(ms, (byte)subject);
               readThingImpl = HandleReadBodyUInt8;
               break;
            case nameof(UInt16):
               writeThingImpl = (ms, subject) => HandleWriteThingUInt16(ms, (ushort)subject);
               readThingImpl = HandleReadBodyUInt16;
               break;
            case nameof(UInt32):
               writeThingImpl = (ms, subject) => HandleWriteThingUInt32(ms, (uint)subject);
               readThingImpl = HandleReadBodyUInt32;
               break;
            case nameof(UInt64):
               writeThingImpl = (ms, subject) => HandleWriteThingUInt64(ms, (ulong)subject);
               readThingImpl = HandleReadBodyUInt64;
               break;
            default:
               throw new NotSupportedException(typeof(T).FullName);
         }
      }

      public byte[] GetWriterBuffer() {
         var result = writerBuffer;
         if (result == null) {
            return writerBuffer = new byte[8];
         }
         return result;
      }

      public void WriteThing(SomeMemoryStreamWrapperThing dest, object subject) {
         writeThingImpl(dest, subject);
      }

      public object ReadBody(BinaryReader reader) {
         return readThingImpl(reader);
      }

      private void HandleWriteThingInt8(SomeMemoryStreamWrapperThing dest, sbyte subject) {
         dest.Write(fullTypeBinaryRepresentationCache.GetOrCompute(typeof(sbyte)));
         var buffer = GetWriterBuffer();
         buffer[0] = (byte)subject;
         dest.Write(buffer, 0, sizeof(sbyte));
      }

      private void HandleWriteThingInt16(SomeMemoryStreamWrapperThing dest, short subject) {
         if (subject < sbyte.MinValue || subject > sbyte.MaxValue) {
            dest.Write(fullTypeBinaryRepresentationCache.GetOrCompute(typeof(short)));

            var buffer = GetWriterBuffer();
            fixed (byte* pBuffer = buffer)
            {
               *(short*)pBuffer = subject;
            }
            dest.Write(buffer, 0, sizeof(short));
         } else {
            HandleWriteThingInt8(dest, (sbyte)subject);
         }
      }

      private void HandleWriteThingInt32(SomeMemoryStreamWrapperThing dest, int subject) {
         if (subject < short.MinValue || subject > short.MaxValue) {
            dest.Write(fullTypeBinaryRepresentationCache.GetOrCompute(typeof(int)));

            var buffer = GetWriterBuffer();
            fixed (byte* pBuffer = buffer)
            {
               *(int*)pBuffer = subject;
            }
            dest.Write(buffer, 0, sizeof(int));
         } else {
            HandleWriteThingInt16(dest, (short)subject);
         }
      }

      private void HandleWriteThingInt64(SomeMemoryStreamWrapperThing dest, long subject) {
         if (subject < int.MinValue || subject > int.MaxValue) {
            dest.Write(fullTypeBinaryRepresentationCache.GetOrCompute(typeof(long)));

            var buffer = GetWriterBuffer();
            fixed (byte* pBuffer = buffer)
            {
               *(long*)pBuffer = subject;
            }
            dest.Write(buffer, 0, sizeof(long));
         } else {
            HandleWriteThingInt32(dest, (int)subject);
         }
      }

      private void HandleWriteThingUInt8(SomeMemoryStreamWrapperThing dest, byte subject) {
         dest.Write(fullTypeBinaryRepresentationCache.GetOrCompute(typeof(byte)));
         var buffer = GetWriterBuffer();
         buffer[0] = subject;
         dest.Write(buffer, 0, sizeof(byte));
      }

      private void HandleWriteThingUInt16(SomeMemoryStreamWrapperThing dest, ushort subject) {
         if (subject > byte.MaxValue) {
            dest.Write(fullTypeBinaryRepresentationCache.GetOrCompute(typeof(ushort)));

            var buffer = GetWriterBuffer();
            fixed (byte* pBuffer = buffer) {
               *(ushort*)pBuffer = subject;
            }
            dest.Write(buffer, 0, sizeof(ushort));
         } else {
            HandleWriteThingUInt8(dest, (byte)subject);
         }
      }

      private void HandleWriteThingUInt32(SomeMemoryStreamWrapperThing dest, uint subject) {
         if (subject > ushort.MaxValue) {
            dest.Write(fullTypeBinaryRepresentationCache.GetOrCompute(typeof(uint)));

            var buffer = GetWriterBuffer();
            fixed (byte* pBuffer = buffer) {
               *(uint*)pBuffer = subject;
            }
            dest.Write(buffer, 0, sizeof(uint));
         } else {
            HandleWriteThingUInt16(dest, (ushort)subject);
         }
      }

      private void HandleWriteThingUInt64(SomeMemoryStreamWrapperThing dest, ulong subject) {
         if (subject > uint.MaxValue) {
            dest.Write(fullTypeBinaryRepresentationCache.GetOrCompute(typeof(ulong)));

            var buffer = GetWriterBuffer();
            fixed (byte* pBuffer = buffer) {
               *(ulong*)pBuffer = subject;
            }
            dest.Write(buffer, 0, sizeof(ulong));
         } else {
            HandleWriteThingUInt32(dest, (uint)subject);
         }
      }

      private object HandleReadBodyInt8(BinaryReader reader) => reader.ReadSByte();
      private object HandleReadBodyInt16(BinaryReader reader) => reader.ReadInt16();
      private object HandleReadBodyInt32(BinaryReader reader) => reader.ReadInt32();
      private object HandleReadBodyInt64(BinaryReader reader) => reader.ReadInt64();

      private object HandleReadBodyUInt8(BinaryReader reader) => reader.ReadByte();
      private object HandleReadBodyUInt16(BinaryReader reader) => reader.ReadUInt16();
      private object HandleReadBodyUInt32(BinaryReader reader) => reader.ReadUInt32();
      private object HandleReadBodyUInt64(BinaryReader reader) => reader.ReadUInt64();
   }
}