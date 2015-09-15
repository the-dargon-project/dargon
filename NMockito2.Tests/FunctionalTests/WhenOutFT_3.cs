using System;
using Xunit;

namespace NMockito2.FunctionalTests {
   public class WhenOutFT_3 : NMockitoInstance {
      [Fact]
      public void Run() {
         var testObj = CreateMock<TestInterface>();
         Expect<int, char, bool>((x, y) => testObj.TryInvoke("asdf", out x, out y))
            .SetOut(1337, 't').ThenReturn(true)
            .SetOut(21337, 'u').ThenReturn(false);

         int a;
         char b;
         AssertTrue(testObj.TryInvoke("asdf", out a, out b));
         AssertEquals(1337, a);
         AssertEquals('t', b);

         AssertFalse(testObj.TryInvoke("asdf", out a, out b));
         AssertEquals(21337, a);
         AssertEquals('u', b);

         VerifyExpectationsAndNoMoreInteractions();
      }

      internal interface TestInterface {
         bool TryInvoke(string x, out int a, out char b);
      }
   }
}
