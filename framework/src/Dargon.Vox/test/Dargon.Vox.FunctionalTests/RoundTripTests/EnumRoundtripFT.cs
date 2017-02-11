using Dargon.Vox.RoundTripTests;
using Dargon.Vox.Utilities;
using Xunit;

namespace Dargon.Vox.FunctionalTests.RoundTripTests {
   public class EnumRoundtripFT : RoundTripTest {
      [Fact]
      public void EnumValuesTest() {
         RunRoundTripTest(RegisteredEnum.A);
         RunRoundTripTest(RegisteredEnum.B);
      }

      [Fact]
      public void EnumArraysTest() {
         RunRoundTripTest(new[] { RegisteredEnum.A, RegisteredEnum.B });
      }

      public enum RegisteredEnum {
         A,
         B
      }
   }
}