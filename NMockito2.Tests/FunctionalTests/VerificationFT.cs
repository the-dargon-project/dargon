using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NMockito2.Verification;
using Xunit;

namespace NMockito2.FunctionalTests {
   public class VerificationFT : NMockitoInstance {
      private readonly TestInterface testObj;

      public VerificationFT() {
         this.testObj = CreateMock<TestInterface>();
      }

      [Fact]
      public void TrivialVerifyNoMoreInteractionsTest() {
         VerifyNoMoreInteractions();
      }

      [Fact]
      public void UnexpectedInvocationTest() {
         testObj.A("20");

         AssertThrows<InvocationExpectationException>(VerifyExpectationsAndNoMoreInteractions);
      }

      [Fact]
      public void ExpectationsNotMetTest() {
         Expect(testObj.A("20")).ThenReturn(10);

         AssertThrows<InvocationExpectationException>(VerifyExpectations);
      }

      [Fact]
      public void ExpectationsAndVerificationsNotMetTest() {
         Expect(testObj.A("20")).ThenReturn(10);

         testObj.A(30);

         AssertThrows<AggregateException>(VerifyExpectationsAndNoMoreInteractions);
      }

      internal interface TestInterface {
         int A<T>(T b);
      }
   }
}
