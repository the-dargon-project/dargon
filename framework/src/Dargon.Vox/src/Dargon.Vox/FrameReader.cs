using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Dargon.Commons;
using Dargon.Commons.Exceptions;
using Dargon.Vox.Utilities;

namespace Dargon.Vox {
   public class FrameReader {
      private readonly ThisIsTotesTheRealLegitThingReaderWriterThing thisIsTotesTheRealLegitThingReaderWriterThing;

      public FrameReader(ThisIsTotesTheRealLegitThingReaderWriterThing thisIsTotesTheRealLegitThingReaderWriterThing) {
         this.thisIsTotesTheRealLegitThingReaderWriterThing = thisIsTotesTheRealLegitThingReaderWriterThing;
      }

      public object ReadFrame(Stream stream, Type hintType) {
         using (var reader = new VoxBinaryReader(stream)) {
            return thisIsTotesTheRealLegitThingReaderWriterThing.ReadThing(reader, hintType);
         }
      }
   }

   public unsafe class VoxBinaryReader : IDisposable {
      private readonly Func<byte> readByte;
      private readonly Func<byte> readByteFromEndOfStreamUnsafe;
      private Stream stream;
      private GCHandle bufferHandle;
      private byte* basePointer = null;
      private byte*[] dataPointers = new byte*[16];
      private int[] bytesRemainings = Util.Generate(16, i => -1);
      private int currentIndex = -1;

      public VoxBinaryReader(Stream stream) {
         readByte = ReadByte;
         readByteFromEndOfStreamUnsafe = __ReadByteFromStreamUnsafe;
         HandleEnterReadFrame(stream);
      }

      public void Dispose() {
         HandleLeaveReadFrame();
      }

      private int AdvanceIndex() {
         var nextIndex = currentIndex + 1;
         if (nextIndex == bytesRemainings.Length) {
            var a = dataPointers;
            var b = bytesRemainings;
            var nextLength = dataPointers.Length * 2;
            dataPointers = new byte*[nextLength];
            bytesRemainings = new int[nextLength];
            Array.Copy(a, dataPointers, a.Length);
            Array.Copy(b, bytesRemainings, b.Length);
         }
         return currentIndex = nextIndex;
      }

      private void AdvanceCurrentBuffer(int length) {
         if (length < 0) {
            throw new ArgumentOutOfRangeException();
         }

         var oldBytesRemaining = bytesRemainings[currentIndex];
         var nextBytesRemaining = oldBytesRemaining - length;
         if (nextBytesRemaining < 0) {
            throw new IndexOutOfRangeException("Attempted to surpass end of buffer.");
         }
         if (oldBytesRemaining < nextBytesRemaining) {
            throw new OverflowException("Advance operation led to bytes remaining underflow.");
         }

         dataPointers[currentIndex] += length;
         bytesRemainings[currentIndex] = nextBytesRemaining;
      }

      private byte __ReadByteFromStreamUnsafe() {
         var c = stream.ReadByte();
         if (c < 0) {
            throw new EndOfStreamException();
         }
         return (byte)c;
      }

      public byte ReadByte() {
         var ptr = dataPointers[currentIndex];
         AdvanceCurrentBuffer(1);
         return *ptr;
      }

      public void ReadBytes(int c, byte* dest) {
         var src = dataPointers[currentIndex];
         AdvanceCurrentBuffer(c);
         Buffer.MemoryCopy(src, dest, c, c);
      }

      public byte* TakeBytes(int c) {
         var src = dataPointers[currentIndex];
         AdvanceCurrentBuffer(c);
         return src;
      }

      public int ReadVariableInt() {
         return VarIntSerializer.ReadVariableInt(readByte);
      }

      public void HandleEnterReadFrame(Stream stream) {
         this.stream = stream;
         var frameLength = VarIntSerializer.ReadVariableInt(readByteFromEndOfStreamUnsafe);
         if (stream.Position + frameLength > stream.Length) {
            throw new InvalidStateException("The frame would surpass the end of the stream.");
         }

         var ms = stream as MemoryStream;
         if (ms != null) {
            var buffer = ms.GetBuffer();
            bufferHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            basePointer = (byte*)bufferHandle.AddrOfPinnedObject().ToPointer();
            dataPointers[0] = basePointer + ms.Position;
            ms.Position += frameLength;
         } else {
            basePointer = (byte*)Marshal.AllocHGlobal(frameLength).ToPointer();
            var ums = new UnmanagedMemoryStream(basePointer, frameLength);

            const int kCopyBufferSize = 4096;
            byte[] copyBuffer = new byte[kCopyBufferSize];
            int bytesRemaining = frameLength;
            while (bytesRemaining > 0) {
               var bytesRead = stream.Read(copyBuffer, 0, Math.Min(bytesRemaining, kCopyBufferSize));
               ums.Write(copyBuffer, 0, bytesRead);
               bytesRemaining -= bytesRead;
            }

            dataPointers[0] = basePointer;
         }
         bytesRemainings[0] = frameLength;
         currentIndex = 0;
      }

      public void HandleLeaveReadFrame() {
         currentIndex = -1;
         bytesRemainings[0] = -1;
         dataPointers[0] = null;
         if (stream is MemoryStream) {
            basePointer = null;
            bufferHandle.Free();
            bufferHandle = default(GCHandle);
         } else {
            Marshal.FreeHGlobal(new IntPtr(basePointer));
            basePointer = null;
         }
         stream = null;
      }

      public void HandleEnterInnerBuffer(int length) {
         var capture = dataPointers[currentIndex];
         AdvanceCurrentBuffer(length);

         var newIndex = AdvanceIndex();
         dataPointers[newIndex] = capture;
         bytesRemainings[newIndex] = length;
      }

      public void HandleLeaveInnerBuffer() {
         dataPointers[currentIndex] = null;
         bytesRemainings[currentIndex] = -1;
         currentIndex--;
      }
   }
}