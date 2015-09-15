using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NMockito2.Fluent;
using Xunit;

namespace NMockito2.FunctionalTests {
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
