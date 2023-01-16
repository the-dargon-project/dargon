using System;
using System.IO;

namespace Dargon.Commons.IO;

public class MemoryByteStream : Stream {
   private readonly Memory<byte> inner;
   private int cursor = 0;

   public MemoryByteStream(Memory<byte> inner) {
      this.inner = inner;
   }

   public override bool CanRead => true;
   public override bool CanSeek => true;
   public override bool CanWrite => true;
   public override long Length => inner.Length;

   public override long Position {
      get => cursor;
      set => cursor = ConservativeIntOverflowGuard(value);
   }

   public override int Read(byte[] buffer, int offset, int count) {
      var src = inner.Slice(cursor).Span;
      var dst = buffer.AsSpan(offset, count);
      var len = Math.Min(src.Length, dst.Length);
      src.CopyTo(dst.Slice(0, len));
      return len;
   }

   public override long Seek(long offset, SeekOrigin origin) {
      var next = origin switch {
         SeekOrigin.Begin => offset,
         SeekOrigin.Current => offset + cursor,
         SeekOrigin.End => inner.Length + offset,
      };

      if (next < 0 || next >= inner.Length) {
         throw new IOException($"Attempted to seek to index {next} (cursor {cursor} origin {origin} offset {offset}) in memory of length {inner.Length}");
      }

      return cursor = ConservativeIntOverflowGuard(next);
   }

   public override void Write(byte[] buffer, int offset, int count) {
      ConservativeIntOverflowGuard(cursor + (long)count);
      buffer.AsSpan(offset, count).CopyTo(inner.Slice(cursor).Span);
   }

   public override void SetLength(long value) => throw new NotSupportedException();

   public override void Flush() { }

   private int ConservativeIntOverflowGuard(long cursor) {
      if (cursor >= (long)int.MaxValue - 100000) {
         throw new OverflowException($"I don't even want to think about this situation. Cursor {cursor}");
      }

      return (int)cursor;
   }
}