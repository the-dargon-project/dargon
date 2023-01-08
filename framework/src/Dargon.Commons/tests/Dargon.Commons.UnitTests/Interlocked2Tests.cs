using NMockito;
using Xunit;

namespace Dargon.Commons {
   public class Interlocked2Tests : NMockitoInstance {
      [Fact]
      public void PreIncrementHappyPath() {
         var i = 0;
         Interlocked2.PreIncrement(ref i).AssertEquals(1);
         i.AssertEquals(1);
      }

      [Fact]
      public void PostIncrementHappyPath() {
         var i = 0;
         Interlocked2.PostIncrement(ref i).AssertEquals(0);
         i.AssertEquals(1);
      }

      [Fact]
      public void PreDecrementHappyPath() {
         var i = 0;
         Interlocked2.PreDecrement(ref i).AssertEquals(-1);
         i.AssertEquals(-1);
      }

      [Fact]
      public void PostDecrementHappyPath() {
         var i = 0;
         Interlocked2.PostDecrement(ref i).AssertEquals(0);
         i.AssertEquals(-1);
      }
   }
}
