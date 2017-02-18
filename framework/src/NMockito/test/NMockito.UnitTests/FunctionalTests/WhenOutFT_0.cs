using System;
using Xunit;

namespace NMockito.FunctionalTests {
   public class WhenOutFT_0 : NMockitoInstance {
      [Fact]
      public void Run() {
         var testObj = CreateMock<TestInterface>();

         object resultOutPlaceholder;
         var outValue = new object();
         Expect(() => testObj.TryInvoke(out resultOutPlaceholder))
            .SetOuts(null).ThenReturn()
            .SetOuts(outValue).ThenReturn()
            .ThenThrow(new InvalidOperationException());

         object result;
         testObj.TryInvoke(out result);
         AssertEquals(null, result);

         testObj.TryInvoke(out result);
         AssertEquals(outValue, result);

         Assert(() => testObj.TryInvoke(out result)).Throws<InvalidOperationException>();

         VerifyExpectationsAndNoMoreInteractions();
      }

      internal interface TestInterface {
         void TryInvoke(out object result);
      }
   }
}
