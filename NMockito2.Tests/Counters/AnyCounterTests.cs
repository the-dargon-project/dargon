using Xunit;

namespace NMockito2.Counters {
   public class AnyCounterTests {
      private readonly AnyCounter testObj = new AnyCounter();

      [Fact]
      public void Remaining_IsIntMaxValue_Test() => Assert.Equal(int.MaxValue, testObj.Remaining);

      [Fact]
      public void IsSatisfied_InitiallyFalse_Test() => Assert.False(testObj.IsSatisfied);

      [Fact]
      public void IsSatisfied_TrueAfterVerification_Test() {
         testObj.HandleVerified(1);
         Assert.True(testObj.IsSatisfied);
      }

      [Fact]
      public void Description_Test() => Assert.Equal("Any", testObj.Description);
   }
}