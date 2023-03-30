using System;
using System.IO;

namespace Dargon.Vox2 {
   public static class VarIntSerializer {
      private const byte kMoreOctetsRemaining = 0x80;
      private const byte kNegativeInteger = 0x40;
      private const byte kSixBitMask = unchecked(~((-1) << 6));
      private const byte kSevenBitMask = unchecked(~((-1) << 7));

      public static void WriteVariableInt(Action<byte> write, int val) {
         // First octet
         byte b = (byte)(val & kSixBitMask);
         if (val < 0) {
            // For negatives, we serialize -n-1, which is nonnegative.
            val = ~val;
            b ^= kSixBitMask;
            b |= kNegativeInteger;
         }

         val = val >> 6;

         // Remaining octets
         while (val > 0) {
            b |= kMoreOctetsRemaining;
            write(b);

            b = (byte)(val & kSevenBitMask);
            val = val >> 7;
         }

         write(b);
      }

      public static int ReadVariableInt(Func<byte> readByte) {
         byte b = readByte();
         var isNegative = (b & kNegativeInteger) != 0;
         int result = b & kSixBitMask;
         int writeOffset = 6;
         while ((b & kMoreOctetsRemaining) != 0) {
            b = readByte();
            result |= (b & kSevenBitMask) << writeOffset;
            writeOffset += 7;
         }

         if (isNegative) {
            result = ~result;
         }

         return result;
      }

      public static void WriteVariableInt(this BinaryWriter bw, int val) {
         // First octet
         byte b = (byte)(val & kSixBitMask);
         if (val < 0) {
            // For negatives, we serialize -n-1, which is nonnegative.
            val = ~val;
            b ^= kSixBitMask;
            b |= kNegativeInteger;
         }

         val = val >> 6;

         // Remaining octets
         while (val > 0) {
            b |= kMoreOctetsRemaining;
            bw.Write((byte)b);

            b = (byte)(val & kSevenBitMask);
            val = val >> 7;
         }

         bw.Write((byte)b);
      }

      public static int ReadVariableInt(this BinaryReader br) {
         byte b = br.ReadByte();
         var isNegative = (b & kNegativeInteger) != 0;
         int result = b & kSixBitMask;
         int writeOffset = 6;
         while ((b & kMoreOctetsRemaining) != 0) {
            b = br.ReadByte();
            result |= (b & kSevenBitMask) << writeOffset;
            writeOffset += 7;
         }

         if (isNegative) {
            result = ~result;
         }

         return result;
      }

      public static byte[] ToVariableIntBytes(this int i) {
         using (var ms = new MemoryStream(5))
         using (var bw = new BinaryWriter(ms)) {
            bw.WriteVariableInt(i);
            return ms.ToArray();
         }
      }
      
      public static byte[] ToVariableIntBytes(this int[] arr) {
         using (var ms = new MemoryStream(5))
         using (var bw = new BinaryWriter(ms)) {
            foreach (var x in arr) {
               bw.WriteVariableInt(x);
            }

            return ms.ToArray();
         }
      }
   }
}