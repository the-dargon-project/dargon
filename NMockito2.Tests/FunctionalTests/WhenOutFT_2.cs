using System;
using Xunit;

namespace NMockito2.FunctionalTests {
   public class WhenOutFT_2 : NMockitoInstance {
      [Fact]
      public void Run() {
         var testObj = CreateMock<TestInterface>();
         Expect<int, bool>(x => testObj.TryInvoke("asdf", out x))
            .SetOut(1337).ThenReturn(true)
            .SetOut(21337).ThenReturn(false);

         int result;
         AssertTrue(testObj.TryInvoke("asdf", out result));
         AssertEquals(1337, result);

         AssertFalse(testObj.TryInvoke("asdf", out result));
         AssertEquals(21337, result);

         VerifyExpectationsAndNoMoreInteractions();
      }

      internal interface TestInterface {
         bool TryInvoke(string x, out int result);
      }
   }
}
