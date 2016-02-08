using System;
using NMockito.Fluent;
using Xunit;

namespace NMockito.FunctionalTests {
   public class ParamsFT : NMockitoInstance {
      [Fact]
      public void Run() {
         var testObj = CreateMock<TestInterface>();
         testObj.Invoke(10, "a", "b").Throws(new InvalidOperationException()).ThenReturns("10ab");
         testObj.Invoke(20, null).Returns("20null");

         Assert(testObj).Invoke(10, "a", "b").Throws<InvalidOperationException>();
         AssertEquals("10ab", testObj.Invoke(10, "a", "b"));
         AssertEquals("20null", testObj.Invoke(20, null));

         VerifyExpectationsAndNoMoreInteractions();
      }

      internal interface TestInterface {
         string Invoke(int n, params string[] parameters);
      }
   }
}
