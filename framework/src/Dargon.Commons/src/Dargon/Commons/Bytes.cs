using System;
using System.Linq;
using System.Text;

namespace Dargon.Commons {
   public static class Bytes {
      public static bool ArraysEqual(byte[] param1, byte[] param2) {
         return ArraysEqual(param1, 0, param1.Length, param2, 0, param2.Length);
      }

      public static bool ArraysEqual(byte[] a, int aOffset, byte[] b, int bOffset, int length) {
         return ArraysEqual(a, aOffset, length, b, bOffset, length);
      }

      public static unsafe bool ArraysEqual(byte[] a, int aOffset, int aLength, byte[] b, int bOffset, int bLength) {
         if (aOffset + aLength > a.Length) {
            throw new IndexOutOfRangeException("aOffset + aLength > a.Length");
         } else if (bOffset + bLength > b.Length) {
            throw new IndexOutOfRangeException("bOffset + bLength > b.Length");
         } else if (aOffset < 0) {
            throw new IndexOutOfRangeException("aOffset < 0");
         } else if (bOffset < 0) {
            throw new IndexOutOfRangeException("bOffset < 0");
         } else if (aLength < 0) {
            throw new IndexOutOfRangeException("aLength < 0");
         } else if (bLength < 0) {
            throw new IndexOutOfRangeException("bLength < 0");
         }

         if (aLength != bLength) {
            return false;
         } else if (a == b && aOffset == bOffset && aLength == bLength) {
            return true;
         }

         fixed (byte* pABase = a)
         fixed (byte* pBBase = b) {
            byte* pACurrent = pABase + aOffset, pBCurrent = pBBase + bOffset;
            return BuffersEqual(pACurrent, pBCurrent, aLength);
         }
      }

      public static unsafe bool BuffersEqual(byte* a, byte* b, int length) {
         var numLongs = length / 8;
         for (var i = 0; i < numLongs; i++) {
            if (*(ulong*)a != *(ulong*)b) {
               return false;
            }
            a += 8;
            b += 8;
         }
         if ((length & 4) != 0) {
            if (*(uint*)a != *(uint*)b) {
               return false;
            }
            a += 4;
            b += 4;
         }
         if ((length & 2) != 0) {
            if (*(ushort*)a != *(ushort*)b) {
               return false;
            }
            a += 2;
            b += 2;
         }
         if ((length & 1) != 0) {
            if (*a != *b) {
               return false;
            }
         }
         return true;
      }

      // http://stackoverflow.com/questions/311165/how-do-you-convert-byte-array-to-hexadecimal-string-and-vice-versa
      public static string ToHexString(this byte[] a) {
         var hex = new StringBuilder(a.Length * 2);
         foreach (byte b in a)
            hex.AppendFormat("{0:x2}", b);
         return hex.ToString();
      }

      public static string ToHexDump(this byte[] a) {
         return string.Join(
            Environment.NewLine,
            a.Chunk(16)
             .Select(chunk => {
                var offset = chunk.Index * 16;
                var hex = string.Join(" ", chunk.Items.Chunk(4).Select(quad => string.Join("", quad.Items.Map(b => b.ToString("X2"))))).PadRight(4 * 8 + 7);
                var ascii = string.Join("", chunk.Items.Map(b => b < 32 || b >= 126 ? '.' : (char)b));
                return $"{offset:X8}  {hex}  {ascii}";
             }));
      }
   }
}