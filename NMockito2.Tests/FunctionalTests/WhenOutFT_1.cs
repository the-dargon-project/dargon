using System;
using NMockito2.Fluent;
using NMockito2.Assertions;
using Xunit;

namespace NMockito2.FunctionalTests {
   public class WhenOutFT_1 : NMockitoInstance {
      [Fact]
      public void Run() {
         var testObj = CreateMock<TestInterface>();
         testObj.TryInvoke("asdf").Returns(true, false).ThenThrows(new InvalidOperationException());

         AssertTrue(testObj.TryInvoke("asdf"));
         AssertFalse(testObj.TryInvoke("asdf"));
         Assert(testObj).TryInvoke("asdf").Throws<InvalidOperationException>();

         VerifyExpectationsAndNoMoreInteractions();
      }

      internal interface TestInterface {
         bool TryInvoke(string x);
      }
   }
}
