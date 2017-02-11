using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dargon.Vox.RoundTripTests;
using Xunit;

namespace Dargon.Vox.FunctionalTests.RoundTripTests {
   public class BooleanRoundtripFT : RoundTripTest {
      private static readonly Type[] testTypes = new Type[0];

      public BooleanRoundtripFT() : base(testTypes) { }

      [Fact]
      public void TrueRoundTripTest() {
         MultiThreadedRoundTripTest(new[] { true }, 10000, 8);
      }

      [Fact]
      public void FalseRoundTripTest() {
         MultiThreadedRoundTripTest(new[] { false }, 10000, 8);
      }

      [Fact]
      public void ThisIsMaybeProbablyTotallyGoingToBreaktest() {
         var testCase = new[] { true, false, true, false, true, false };
         MultiThreadedRoundTripTest(new[] { testCase }, 10000, 8);
      }
   }
}
