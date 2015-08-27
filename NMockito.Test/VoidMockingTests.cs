using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NMockito {
   public class VoidMockingTests : NMockitoInstance {
      [Fact]
      public void WithoutParameter_Test() {
         var mock = CreateMock<TestInterface>();
         When(() => mock.Method()).ThenThrow(new InternalException());
         AssertThrows<InternalException>(() => mock.Method());
         Verify(mock).Method();
         VerifyNoMoreInteractions();
      }

      [Fact]
      public void WithParameter_Test() {
         var mock = CreateMock<TestInterface>();
         When(() => mock.Method(10)).ThenThrow(new InternalException());
         AssertThrows<InternalException>(() => mock.Method(10));
         Verify(mock).Method(10);
         VerifyNoMoreInteractions();
      }

      public interface TestInterface {
         void Method();
         void Method(int value);
      }
      private class InternalException : Exception { }
   }
}
