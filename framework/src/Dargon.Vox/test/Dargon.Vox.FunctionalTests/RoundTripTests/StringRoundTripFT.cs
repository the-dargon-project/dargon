using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dargon.Vox.RoundTripTests;
using Xunit;

namespace Dargon.Vox.FunctionalTests.RoundTripTests {
   public class StringRoundTripFT : RoundTripTest {
      private static readonly Type[] testTypes = new Type[0];

      public StringRoundTripFT() : base(testTypes) { }

      [Fact]
      public void StringRoundTripTest() {
         var cases = CreatePlaceholder<string[]>();
         MultiThreadedRoundTripTest(cases, 10000, 8);
      }

      [Fact]
      public void StringArrayRoundTripTest() {
         var cases = CreatePlaceholder<string[][]>();
         MultiThreadedRoundTripTest(cases, 10000, 8);
      }
   }

   public class ObjectRoundTripFT : RoundTripTest {
      [Fact]
      public void Run() {
         var cases = new object[] {
            null,
            new object()
         };
         MultiThreadedRoundTripTest(
            cases, 20000, 8,
            (a, b) => {
               AssertTrue(
                  (a == null && b == null) ||
                  (a.GetType() == b.GetType() && a.GetType() == typeof(object)));
            });
      }
   }
}
