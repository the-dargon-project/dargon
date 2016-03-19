﻿using Dargon.Vox.Internals;

namespace Dargon.Vox.Data {
   public static class VariableIntegerSerializationStatics {
      private const byte kMoreOctetsRemaining = 0x80;
      private const byte kNegativeInteger = 0x40;
      private const byte kSixBitMask = unchecked(~((-1) << 6));
      private const byte kSevenBitMask = unchecked(~((-1) << 7));

      public static void WriteTypeId(this IForwardDataWriter writer, TypeId typeId) {
         WriteVariableInt(writer, (int)typeId);
      }

      public static void WriteVariableInt(this IForwardDataWriter writer, int val) {
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
            writer.WriteByte(b);

            b = (byte)(val & kSevenBitMask);
            val = val >> 7;
         }
         writer.WriteByte(b);
      }

      public static TypeId ReadTypeId(this IForwardDataReader reader) {
         return (TypeId)ReadVariableInt(reader);
      }

      public static int ReadVariableInt(this IForwardDataReader reader) {
         int throwaway;
         return reader.ReadVariableInt(out throwaway);
      }

      public static int ReadVariableInt(this IForwardDataReader reader, out int bytesRead) {
         bytesRead = 1;
         byte b = reader.ReadByte();
         var isNegative = (b & kNegativeInteger) != 0;
         int result = b & kSixBitMask;
         int writeOffset = 6;
         while ((b & kMoreOctetsRemaining) != 0) {
            bytesRead++;
            b = reader.ReadByte();
            result |= (b & kSevenBitMask) << writeOffset;
            writeOffset += 7;
         }
         if (isNegative) {
            result = ~result;
         }
         return result;
      }

      public static int ComputeTypeIdLength(this TypeId x) {
         return ComputeVariableIntLength((int)x);
      }

      public static int ComputeVariableIntLength(this int x) {
         if (x < 0) x = ~x;
         const int fiveMask = kSevenBitMask << (6 + 3 * 7);
         if ((x & fiveMask) != 0) return 5;
         const int fourMask = kSevenBitMask << (6 + 2 * 7);
         if ((x & fourMask) != 0) return 4;
         const int threeMask = kSevenBitMask << (6 + 1 * 7);
         if ((x & threeMask) != 0) return 3;
         const int twoMask = kSevenBitMask << 6;
         if ((x & twoMask) != 0) return 2;
         return 1;
      }
   }
}
