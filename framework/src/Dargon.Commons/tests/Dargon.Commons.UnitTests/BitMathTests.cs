using NMockito;
using Xunit;

namespace Dargon.Commons {
   public class BitMathTests : NMockitoInstance {
      [Fact]
      public void GetMSB() {
         AssertEquals(0b000U, BitMath.GetMSB(0b000));
         AssertEquals(0b001U, BitMath.GetMSB(0b001));
         AssertEquals(0b010U, BitMath.GetMSB(0b010));
         AssertEquals(0b010U, BitMath.GetMSB(0b011));
         AssertEquals(0b100U, BitMath.GetMSB(0b100));
         AssertEquals(0b100U, BitMath.GetMSB(0b101));
         AssertEquals(0b100U, BitMath.GetMSB(0b110));
         AssertEquals(0b100U, BitMath.GetMSB(0b111));
      }

      [Fact]
      public void GetMSBIndex() {
         AssertEquals(-1, BitMath.GetMSBIndex(0b000));
         AssertEquals(0, BitMath.GetMSBIndex(0b001));
         AssertEquals(1, BitMath.GetMSBIndex(0b010));
         AssertEquals(1, BitMath.GetMSBIndex(0b011));
         AssertEquals(2, BitMath.GetMSBIndex(0b100));
         AssertEquals(2, BitMath.GetMSBIndex(0b101));
         AssertEquals(2, BitMath.GetMSBIndex(0b110));
         AssertEquals(2, BitMath.GetMSBIndex(0b111));
      }

      [Fact]
      public void CeilingPow2() {
         AssertEquals(0b0000U, BitMath.CeilingPow2(0b0000));
         AssertEquals(0b0001U, BitMath.CeilingPow2(0b0001));
         AssertEquals(0b0010U, BitMath.CeilingPow2(0b0010));
         AssertEquals(0b0100U, BitMath.CeilingPow2(0b0011));
         AssertEquals(0b0100U, BitMath.CeilingPow2(0b0100));
         AssertEquals(0b1000U, BitMath.CeilingPow2(0b0101));
         AssertEquals(0b1000U, BitMath.CeilingPow2(0b0110));
         AssertEquals(0b1000U, BitMath.CeilingPow2(0b0111));
         AssertEquals(0b1000U, BitMath.CeilingPow2(0b1000));
      }
   }
}
