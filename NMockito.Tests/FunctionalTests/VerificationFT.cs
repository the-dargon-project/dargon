using System;
using NMockito.Verification;
using Xunit;

namespace NMockito.FunctionalTests {
   public class VerificationFT : NMockitoInstance {
      private readonly TestInterface testObj;

      public VerificationFT() {
         this.testObj = CreateMock<TestInterface>();
      }

      [Fact]
      public void VerifyNoMoreInteractionsTest() {
         testObj.A(10);
         testObj.A(15);

         AssertThrows<InvocationExpectationException>(VerifyNoMoreInteractions);

         Verify(() => testObj.A(10));
         AssertThrows<InvocationExpectationException>(VerifyNoMoreInteractions);

         Verify(testObj).A(15);
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
