using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dargon.Vox.RoundTripTests;
using Xunit;

namespace Dargon.Vox.FunctionalTests.RoundTripTests {
   public class IntRoundtripFT : RoundTripTest {
      private static readonly long[] kEdgeCaseNumbers = new[] {
         long.MinValue,
         long.MinValue + 1,
         int.MinValue,
         int.MinValue + 1,
         short.MinValue - 1,
         short.MinValue,
         short.MinValue + 1,
         byte.MinValue - 1,
         byte.MinValue,
         byte.MinValue + 1,
         -(2 << 7) - 1,
         -(2 << 7),
         -(2 << 7) + 1,
         -(2 << 6) - 1,
         -(2 << 6),
         -(2 << 6) + 1,
         (2 << 6) - 1,
         (2 << 6),
         (2 << 6) + 1,
         (2 << 7) - 1,
         (2 << 7),
         (2 << 7) + 1,
         byte.MaxValue - 1,
         byte.MaxValue,
         byte.MaxValue + 1,
         short.MaxValue - 1,
         short.MaxValue,
         short.MaxValue + 1,
         int.MaxValue - 1,
         int.MaxValue,
         long.MaxValue - 1,
         long.MaxValue
      };

      [Fact]
      public void EdgeCaseTest() {
         var testCases = kEdgeCaseNumbers;
         MultiThreadedRoundTripTest(testCases, 10000, 8);
      }
   }
}
