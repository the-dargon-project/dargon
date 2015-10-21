using System;
using Xunit;

namespace NMockito.FunctionalTests {
   public class VoidFT_0 : NMockitoInstance {
      [Fact]
      public void Run() {
         var mock = CreateMock<TestInterface>();
         Expect(mock.Run).ThenThrow(new InvalidOperationException(), new ArgumentException());
         Assert(mock.Run).Throws<InvalidOperationException>();
         Assert(mock.Run).Throws<ArgumentException>();
         VerifyExpectationsAndNoMoreInteractions();
      }

      internal interface TestInterface {
         void Run();
      }
   }
}
