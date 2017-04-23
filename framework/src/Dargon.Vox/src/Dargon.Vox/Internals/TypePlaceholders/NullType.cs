using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dargon.Vox.Internals.TypePlaceholders {
   public class TypePlaceholderNull { }
   public class TypePlaceholderBoolTrue { }
   public class TypePlaceholderBoolFalse { }

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
