using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NMockito.Verification;
using Xunit;

namespace NMockito.FunctionalTests {
   public class MockTrainingTests : NMockitoInstance {
      [Fact]
      public void UnfulfilledInvocationTest() {
         var mock = CreateMock<TestInterface>(m =>
            m.X == 10 &&
            m.Y == 20);

         var throwaway = mock.X;

         AssertThrows<InvocationExpectationException>(VerifyExpectations);
      }

      [Fact]
      public void UnexpectedInvocationTest() {
         var mock = CreateMock<TestInterface>(m =>
            m.Y == 20);

         var throwaway = mock.X;

         AssertThrows<InvocationExpectationException>(VerifyNoMoreInteractions);
      }

      [Fact]
      public void HappyPathTest() {
         var mock = CreateMock<TestInterface>(m =>
            m.X == 10 &&
            m.Y == 20);

         var throwawayX = mock.X;
         var throwawayY = mock.Y;

         VerifyExpectationsAndNoMoreInteractions();
      }

      public interface TestInterface {
         int X { get; }
         int Y { get; }
      }
   }
}
