using System;
using System.Text;
using Dargon.Commons.Exceptions;
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

      public void WriteThing(VoxBinaryWriter dest, object subject) {
         var type = TypeSimplifier.SimplifyType((Type)subject);
         var typeBinaryRepresentation = fullTypeBinaryRepresentationCache.GetOrCompute(type);
         dest.Write(fullTypeBinaryRepresentationCache.GetOrCompute(typeof(Type)));
         dest.Write(typeBinaryRepresentation);
      }

      public object ReadBody(VoxBinaryReader reader) {
         return typeReader.ReadType(reader);
      }
   }

   public class StringThingReaderWriter : IThingReaderWriter {
      private readonly FullTypeBinaryRepresentationCache fullTypeBinaryRepresentationCache;

      public StringThingReaderWriter(FullTypeBinaryRepresentationCache fullTypeBinaryRepresentationCache) {
         this.fullTypeBinaryRepresentationCache = fullTypeBinaryRepresentationCache;
      }

      public void WriteThing(VoxBinaryWriter dest, object subject) {
         dest.Write(fullTypeBinaryRepresentationCache.GetOrCompute(typeof(string)));

         var str = (string)subject;
         using (dest.ReserveLength()) {
            var bytes = Encoding.UTF8.GetBytes(str);
            dest.Write(bytes);
         }
      }

      public unsafe object ReadBody(VoxBinaryReader reader) {
         var length = VarIntSerializer.ReadVariableInt(reader.ReadByte);
         var pString = reader.TakeBytes(length);
         return Encoding.UTF8.GetString(pString, length);
      }
   }

   public class BoolThingReaderWriter : IThingReaderWriter {
      public void WriteThing(VoxBinaryWriter dest, object subject) {
         throw new InvalidStateException($"WriteThing invoked on {nameof(BoolThingReaderWriter)}");
      }

      public object ReadBody(VoxBinaryReader reader) {
         throw new InvalidStateException($"ReadBody invoked on {nameof(BoolThingReaderWriter)}");
      }
   }

   public class BoolTrueThingReaderWriter : IThingReaderWriter {
      private readonly FullTypeBinaryRepresentationCache fullTypeBinaryRepresentationCache;

      public BoolTrueThingReaderWriter(FullTypeBinaryRepresentationCache fullTypeBinaryRepresentationCache) {
         this.fullTypeBinaryRepresentationCache = fullTypeBinaryRepresentationCache;
      }

      public void WriteThing(VoxBinaryWriter dest, object subject) {
         dest.Write(fullTypeBinaryRepresentationCache.GetOrCompute(typeof(TypePlaceholderBoolTrue)));
      }

      public object ReadBody(VoxBinaryReader reader) {
         return true;
      }
   }

   public class BoolFalseThingReaderWriter : IThingReaderWriter {
      private readonly FullTypeBinaryRepresentationCache fullTypeBinaryRepresentationCache;

      public BoolFalseThingReaderWriter(FullTypeBinaryRepresentationCache fullTypeBinaryRepresentationCache) {
         this.fullTypeBinaryRepresentationCache = fullTypeBinaryRepresentationCache;
      }

      public void WriteThing(VoxBinaryWriter dest, object subject) {
         dest.Write(fullTypeBinaryRepresentationCache.GetOrCompute(typeof(TypePlaceholderBoolFalse)));
      }

      public object ReadBody(VoxBinaryReader reader) {
         return false;
      }
   }

   public unsafe class FloatThingReaderWriter : IThingReaderWriter {
      private readonly FullTypeBinaryRepresentationCache fullTypeBinaryRepresentationCache;
      [ThreadStatic] private static byte[] writerBuffers;

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

      public void WriteThing(VoxBinaryWriter dest, object subject) {
         dest.Write(fullTypeBinaryRepresentationCache.GetOrCompute(typeof(float)));

         var buffer = GetWriterBuffer();
         fixed (byte* pBuffer = buffer) {
            *(float*)pBuffer = (float)subject;
         }
         dest.Write(buffer);
      }

      public object ReadBody(VoxBinaryReader reader) {
         return *(float*)reader.TakeBytes(4);
      }
   }

   public unsafe class IntegerLikeThingReaderWriter<T> : IThingReaderWriter {
      private readonly FullTypeBinaryRepresentationCache fullTypeBinaryRepresentationCache;
      private readonly Action<VoxBinaryWriter, object> writeThingImpl;
      private readonly Func<VoxBinaryReader, object> readThingImpl;
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

      public void WriteThing(VoxBinaryWriter dest, object subject) {
         writeThingImpl(dest, subject);
      }

      public object ReadBody(VoxBinaryReader reader) {
         return readThingImpl(reader);
      }

      private void HandleWriteThingInt8(VoxBinaryWriter dest, sbyte subject) {
         dest.Write(fullTypeBinaryRepresentationCache.GetOrCompute(typeof(sbyte)));
         var buffer = GetWriterBuffer();
         buffer[0] = (byte)subject;
         dest.Write(buffer, 0, sizeof(sbyte));
      }

      private void HandleWriteThingInt16(VoxBinaryWriter dest, short subject) {
         if (subject < sbyte.MinValue || subject > sbyte.MaxValue) {
            dest.Write(fullTypeBinaryRepresentationCache.GetOrCompute(typeof(short)));

            var buffer = GetWriterBuffer();
            fixed (byte* pBuffer = buffer) {
               *(short*)pBuffer = subject;
            }
            dest.Write(buffer, 0, sizeof(short));
         } else {
            HandleWriteThingInt8(dest, (sbyte)subject);
         }
      }

      private void HandleWriteThingInt32(VoxBinaryWriter dest, int subject) {
         if (subject < short.MinValue || subject > short.MaxValue) {
            dest.Write(fullTypeBinaryRepresentationCache.GetOrCompute(typeof(int)));

            var buffer = GetWriterBuffer();
            fixed (byte* pBuffer = buffer) {
               *(int*)pBuffer = subject;
            }
            dest.Write(buffer, 0, sizeof(int));
         } else {
            HandleWriteThingInt16(dest, (short)subject);
         }
      }

      private void HandleWriteThingInt64(VoxBinaryWriter dest, long subject) {
         if (subject < int.MinValue || subject > int.MaxValue) {
            dest.Write(fullTypeBinaryRepresentationCache.GetOrCompute(typeof(long)));

            var buffer = GetWriterBuffer();
            fixed (byte* pBuffer = buffer) {
               *(long*)pBuffer = subject;
            }
            dest.Write(buffer, 0, sizeof(long));
         } else {
            HandleWriteThingInt32(dest, (int)subject);
         }
      }

      private void HandleWriteThingUInt8(VoxBinaryWriter dest, byte subject) {
         dest.Write(fullTypeBinaryRepresentationCache.GetOrCompute(typeof(byte)));
         var buffer = GetWriterBuffer();
         buffer[0] = subject;
         dest.Write(buffer, 0, sizeof(byte));
      }

      private void HandleWriteThingUInt16(VoxBinaryWriter dest, ushort subject) {
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

      private void HandleWriteThingUInt32(VoxBinaryWriter dest, uint subject) {
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

      private void HandleWriteThingUInt64(VoxBinaryWriter dest, ulong subject) {
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

      private object HandleReadBodyInt8(VoxBinaryReader reader) {
         return *(sbyte*)reader.TakeBytes(1);
      }

      private object HandleReadBodyInt16(VoxBinaryReader reader) {
         return *(short*)reader.TakeBytes(2);
      }

      private object HandleReadBodyInt32(VoxBinaryReader reader) {
         return *(int*)reader.TakeBytes(4);
      }

      private object HandleReadBodyInt64(VoxBinaryReader reader) {
         return *(long*)reader.TakeBytes(8);
      }

      private object HandleReadBodyUInt8(VoxBinaryReader reader) {
         return *(byte*)reader.TakeBytes(1);
      }

      private object HandleReadBodyUInt16(VoxBinaryReader reader) {
         return *(ushort*)reader.TakeBytes(2);
      }

      private object HandleReadBodyUInt32(VoxBinaryReader reader) {
         return *(uint*)reader.TakeBytes(4);
      }

      private object HandleReadBodyUInt64(VoxBinaryReader reader) {
         return *(ulong*)reader.TakeBytes(8);
      }
   }
}