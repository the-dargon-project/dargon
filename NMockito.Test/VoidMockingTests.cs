using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NMockito {
   public class VoidMockingTests : NMockitoInstance {
      [Fact]
      public void Run() {
         var mock = CreateMock<TestInterface>();
         When(() => mock.Method(10)).ThenThrow(new InternalException());
         AssertThrows<InternalException>(() => mock.Method(10));
         Verify(mock).Method(10);
         VerifyNoMoreInteractions();
      }

      public interface TestInterface {
         void Method(int value);
      }
      private class InternalException : Exception { }
   }
}
