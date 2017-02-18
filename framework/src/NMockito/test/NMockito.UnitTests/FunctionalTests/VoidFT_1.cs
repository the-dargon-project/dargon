using System;
using Xunit;

namespace NMockito.FunctionalTests {
   public class VoidFT_1 : NMockitoInstance {
      [Fact]
      public void Run() {
         var mock = CreateMock<TestInterface>();
         Expect(() => mock.Run("asdf")).ThenThrow(new InvalidOperationException(), new ArgumentException());
         Assert(() => mock.Run("asdf")).Throws<InvalidOperationException>();
         Assert(() => mock.Run("asdf")).Throws<ArgumentException>();
         VerifyExpectationsAndNoMoreInteractions();
      }

      internal interface TestInterface {
         void Run(string x);
      }
   }
}
