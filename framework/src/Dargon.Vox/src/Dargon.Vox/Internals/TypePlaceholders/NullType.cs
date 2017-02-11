using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dargon.Vox.Internals.TypePlaceholders {
   public class TNull { }
   public class TBoolTrue { }
   public class TBoolFalse { }

   public class ByteArraySlice {
      public ByteArraySlice(byte[] buffer, int offset, int length) {
         Buffer = buffer;
         Offset = offset;
         Length = length;
      }

      public byte[] Buffer { get; }
      public int Offset { get; }
      public int Length { get; }
   }
}
