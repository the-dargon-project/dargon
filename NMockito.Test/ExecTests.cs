using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NMockito {
   public class ExecTests : NMockitoInstance {
      [Mock] private readonly DependencyInterface dependency = null;
      [Mock] private readonly OtherDependencyInterface otherDependency = null;

      private readonly TestClass testObj;

      public ExecTests() {
         testObj = new TestClass(dependency, otherDependency);
      }

      [Fact]
      public void Run() {
         var doneLatch = new CountdownEvent(1);
         When(dependency.GetNext()).ThenReturn(
            Task.FromResult(new int?(1)),
            Task.FromResult(new int?(2))
            ).Exec(() => doneLatch.Signal()).ThenReturn(Task.FromResult<int?>(null));

         testObj.Start();

         AssertTrue(doneLatch.Wait(TimeSpan.FromSeconds(2)));
         Verify(dependency, Times(3)).GetNext();
         Verify(otherDependency, Once(), Whenever()).DoSomething(1);
         Verify(otherDependency, Once(), AfterPrevious()).DoSomething(2);
         VerifyNoMoreInteractions();
      }

      private class TestClass {
         private readonly DependencyInterface dependency;
         private readonly OtherDependencyInterface otherDependencyInterface;

         public TestClass(DependencyInterface dependency, OtherDependencyInterface otherDependencyInterface) {
            this.dependency = dependency;
            this.otherDependencyInterface = otherDependencyInterface;
         }

         public void Start() {
            Task.Run(() => Run());
         }

         private async Task Run() {
            while (true) {
               var next = await dependency.GetNext();
               if (!next.HasValue)
                  break;
               otherDependencyInterface.DoSomething(next.Value);
            }
         }
      }

      public interface DependencyInterface {
         Task<int?> GetNext();
      }

      public interface OtherDependencyInterface {
         void DoSomething(int x);
      }
   }
}
