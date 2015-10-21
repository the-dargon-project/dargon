using System;
using NMockito.Fluent;
using Xunit;

namespace NMockito.FunctionalTests {
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

      [Fact]
      public void ReturnsWithParamsTest() {
         var testObj = CreateMock<TestInterface>();

         testObj.A().Returns(null).ThenReturns(new string[0]).ThenReturns("asdf", "jkl");
         testObj.B().Returns(null).ThenReturns(new string[0]);

         AssertNull(testObj.A());
         AssertEquals("asdf", testObj.A());
         AssertEquals("jkl", testObj.A());

         AssertNull(testObj.B());
         AssertEquals(0, testObj.B().Length);
      }

      internal interface TestInterface {
         bool TryInvoke(string x);
         string A();
         string[] B();
      }
   }
}
