using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dargon.Courier.Utilities {
   public static class VoxSerializationQuirks {
      // Vox serializes integers efficiently (i.e. (int)1 takes 1 byte) but without proper type
      // information, so deserialization of 1 results in (byte)1 unless a hintType is specified.
      // As our RmiResponseDto provides no such hint type, we handle casting here instead.
      public static object CastToDesiredTypeIfIntegerLike(object val, Type desiredType) {
         if (desiredType.IsByRef) desiredType = desiredType.GetElementType();
         if (desiredType == typeof(sbyte) || desiredType == typeof(short) || desiredType == typeof(int) || desiredType == typeof(long) ||
             desiredType == typeof(byte) || desiredType == typeof(ushort) || desiredType == typeof(uint) || desiredType == typeof(ulong)) {
            return Convert.ChangeType(val, desiredType);
         }
         return val;
      }
   }
}
