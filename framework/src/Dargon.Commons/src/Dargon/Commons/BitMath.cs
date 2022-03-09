using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Dargon.Commons {
   public static class BitMath {
      static BitMath() {
         GetMSB(0U).AssertEquals(0U);
         GetMSB(1U).AssertEquals(1U);
         GetMSB(2U).AssertEquals(2U);
         GetMSB(3U).AssertEquals(2U);
         GetMSB(4U).AssertEquals(4U);
         GetMSB(7U).AssertEquals(4U);
         GetMSB(8U).AssertEquals(8U);

         GetMSBIndex(0U).AssertEquals(-1);
         GetMSBIndex(1U).AssertEquals(0);
         GetMSBIndex(2U).AssertEquals(1);
         GetMSBIndex(3U).AssertEquals(1);
         GetMSBIndex(4U).AssertEquals(2);
         GetMSBIndex(5U).AssertEquals(2);
         GetMSBIndex(7U).AssertEquals(2);
         GetMSBIndex(8U).AssertEquals(3);

         CeilingPow2(0U).AssertEquals(0U);
         CeilingPow2(1U).AssertEquals(1U);
         CeilingPow2(2U).AssertEquals(2U);
         CeilingPow2(3U).AssertEquals(4U);
         CeilingPow2(4U).AssertEquals(4U);
         CeilingPow2(5U).AssertEquals(8U);
         CeilingPow2(6U).AssertEquals(8U);
         CeilingPow2(7U).AssertEquals(8U);
         CeilingPow2(8U).AssertEquals(8U);
         CeilingPow2(9U).AssertEquals(16U);
      }

      public static uint GetMSB(uint n) {
         if (n == 0) {
            return 0;
         } else {
            return 1U << BitOperations.Log2(n);
         }
      }

      public static int GetMSBIndex(uint n) {
         return n == 0 ? -1 : BitOperations.Log2(n);
      }

      public static uint CeilingPow2(uint n) {
         var msb = GetMSB(n);
         return n == msb ? msb : msb << 1;
      }
   }
}
