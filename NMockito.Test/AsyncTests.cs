using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NMockito {
   public class AsyncTests : NMockitoInstance {
      [Mock] private readonly DummyInterface dependency = null;

      private readonly TestClass testObj;

      public AsyncTests() {
         testObj = new TestClass(dependency);
      }

      [Fact]
      public async void Run() {
         When(dependency.X()).ThenReturn(Task.FromResult(10));
         When(dependency.Y(10)).ThenReturn(Task.FromResult(20));
         AssertEquals(await testObj.TestMethod(), 20);
         await Verify(dependency, Once(), AfterPrevious()).X();
         await Verify(dependency, Once(), AfterPrevious()).Y(10);
         VerifyNoMoreInteractions();
      }
   }

   public class TestClass {
      private readonly DummyInterface dependency;

      public TestClass(DummyInterface dependency) {
         this.dependency = dependency;
      }

      public async Task<int> TestMethod() {
         var result = await dependency.X();
         return await dependency.Y(result);
      }
   }

   public interface DummyInterface {
      Task<int> X();
      Task<int> Y(int x);
   }
}
