using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Vox.RoundTripTests;
using NMockito;
using Xunit;

namespace Dargon.Vox.FunctionalTests.RoundTripTests {
   public class FloatingPointRoundtripFT : RoundTripTest {
      [Fact]
      public void SingleRoundTripTest() {
         var testCases = new[] {
            float.NaN,
            float.NegativeInfinity,
            float.PositiveInfinity,
            1.0f,
            float.Epsilon,
            1337.0f
         };
         MultiThreadedRoundTripTest(testCases, 20000, 8);
      }

      [Fact]
      public void DoubleRoundTripTest() {
         var testCases = new[] {
            double.NaN,
            double.NegativeInfinity,
            double.PositiveInfinity,
            1.0,
            double.Epsilon,
            1337.0
         };
         MultiThreadedRoundTripTest(testCases, 20000, 8);
      }
   }

   public class GuidRoundTripFT : RoundTripTest {
      [Fact]
      public void Run() {
         var testCases = new[] {
            Guid.Parse("3A43FAF8-A47B-49A9-9BBB-76C56679CE2F"),
            Guid.Parse("58739AE5-2C8B-44CF-81FC-F46D98D688D8"),
            Guid.Parse("B1666845-31B8-479E-8E60-6D259F9931FB"),
            Guid.Parse("1CE65E13-1DEA-43F8-B62D-EBEF3ADC608E"),
            Guid.Parse("DDD0A7A3-1185-4508-9D57-BC9CDCA8120A")
         };
         MultiThreadedRoundTripTest(testCases, 10000, 8);
      }
   }

   public class DateTimeRoundTripFT : RoundTripTest {
      [Fact]
      public void Run() {
         var testCases = Util.Generate(10, i => DateTime.FromBinary(i * 1000000000000000L));
         MultiThreadedRoundTripTest(testCases, 10000, 8);
      }
   }

   public class TimeSpanRoundTripFT : RoundTripTest {
      [Fact]
      public void Run() {
         var testCases = Util.Generate(10, i => TimeSpan.FromTicks(i * 100000000000000L));
         MultiThreadedRoundTripTest(testCases, 10000, 8);
      }
   }
}
