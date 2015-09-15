using System;
using NMockito2.Fluent;
using Xunit;

namespace NMockito2.FunctionalTests {
   public class WhenOutFT_0 : NMockitoInstance {
      [Fact]
      public void Run() {
         var testObj = CreateMock<TestInterface>();
         testObj.TryInvoke().Returns(true, false);

         AssertTrue(testObj.TryInvoke());
         AssertFalse(testObj.TryInvoke());

         VerifyExpectationsAndNoMoreInteractions();
      }

      internal interface TestInterface {
         bool TryInvoke();
      }
   }
}
