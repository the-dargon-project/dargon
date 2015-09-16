using Xunit;

namespace NMockito2.Counters {
   public class TimesCounterTests {
      private const int kInitialCount = 10;

      private readonly TimesCounter testObj = new TimesCounter(kInitialCount);
      
      [Fact]
      public void Run() {
         Assert.Equal(kInitialCount, testObj.Remaining);
         Assert.Equal(false, testObj.IsSatisfied);
         Assert.Equal(kInitialCount.ToString(), testObj.Description);

         const int kNextCount = 7;
         testObj.HandleVerified(3);

         Assert.Equal(kNextCount, testObj.Remaining);
         Assert.Equal(false, testObj.IsSatisfied);
         Assert.Equal(kNextCount.ToString(), testObj.Description);

         const int kFinalCount = 0;
         testObj.HandleVerified(7);

         Assert.Equal(kFinalCount, testObj.Remaining);
         Assert.Equal(true, testObj.IsSatisfied);
         Assert.Equal(kFinalCount.ToString(), testObj.Description);
      }
   }
}