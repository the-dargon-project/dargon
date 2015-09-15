using NMockito2.Fluent;
using System;
using Xunit;

namespace NMockito2.FunctionalTests {
   public class VoidFT_1 : NMockitoInstance {
      [Fact]
      public void Run() {
         var mock = CreateMock<TestInterface>();
//         Assert(mock).Run("asdf").Throws(new InvalidOperationException(), new ArgumentException()));
//         Assert(mock.Run).Throws<InvalidOperationException>();
//         Assert(mock.Run).Throws<ArgumentException>();
         VerifyExpectationsAndNoMoreInteractions();
      }

      internal interface TestInterface {
         void Run(string x);
      }
   }
}
