using System;
using NMockito2.Fluent;
using Xunit;

namespace NMockito2.FunctionalTests {
   public class WhenOutFT_0 : NMockitoInstance {
      [Fact]
      public void Run() {
         var testObj = CreateMock<TestInterface>();
         testObj.TryInvoke().Returns(true, false).ThenThrows(new InvalidOperationException());

         AssertTrue(testObj.TryInvoke());
         AssertFalse(testObj.TryInvoke());
         Assert(testObj).TryInvoke().Throws<InvalidOperationException>();

         VerifyExpectationsAndNoMoreInteractions();
      }

      internal interface TestInterface {
         bool TryInvoke();
      }
   }
}
