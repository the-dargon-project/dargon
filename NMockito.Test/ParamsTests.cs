using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NMockito {
   public class ParamsTests : NMockitoInstance {
      [Fact]
      public void Trivial_Test() {
         var mock = CreateMock<DummyInterface>();
         When(mock.Method(1)).ThenReturn(10);
         var result = mock.Method(1);
         Verify(mock).Method(1);
         AssertEquals(10, result);
      }

      [Fact]
      public void ArrayPassed_Test() {
         var args = new object[] { 1, "Hello" };
         var mock = CreateMock<DummyInterface>();
         When(mock.Method(1, args)).ThenReturn(10);
         var result = mock.Method(1, args);
         Verify(mock).Method(1, args);
         AssertEquals(10, result);
      }

      [Fact]
      public void Complicated_HappyTest() {
         var mock = CreateMock<DummyInterface>();
         When(mock.Method(1, 2, "Hello")).ThenReturn(10);
         var result = mock.Method(1, 2, "Hello");
         Verify(mock).Method(1, 2, "Hello");
         AssertEquals(10, result);
      }

      [Fact]
      public void Complicated_SadTest() {
         var mock = CreateMock<DummyInterface>();
         When(mock.Method(1, 2, "Hello")).ThenReturn(10);
         var result = mock.Method(1, 2, "Goodbye");
         Verify(mock).Method(1, 2, "Goodbye");
         AssertEquals(0, result);
      }

      public interface DummyInterface {
         int Method(int number, params object[] args);
      }
   }
}
