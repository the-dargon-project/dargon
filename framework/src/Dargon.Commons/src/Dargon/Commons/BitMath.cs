using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dargon.Commons {
   public static class BitMath {
      public static uint GetMSB(uint n) {
         n |= (n >> 1);
         n |= (n >> 2);
         n |= (n >> 4);
         n |= (n >> 8);
         n |= (n >> 16);
         return n - (n >> 1);
      }

      public static int GetMSBIndex(uint n) {
         int c = 0;
         while (n != 0) {
            c++;
            n >>= 1;
         }
         return c;
      }

      public static uint CeilingPow2(uint n) {
         var msb = GetMSB(n);
         return n == msb ? msb : msb << 1;
      }
   }
}
