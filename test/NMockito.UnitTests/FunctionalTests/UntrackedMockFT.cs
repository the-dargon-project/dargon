using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NMockito.Attributes;
using Xunit;

namespace NMockito.FunctionalTests {
   public class UntrackedMockFT : NMockitoInstance {
      [Mock(IsTracked = false)] private readonly TestInterface testInterface = null;

      [Fact]
      public void UntrackedMockDoesNotRegisterInteractions() {
         testInterface.DoSomething();
         VerifyNoMoreInteractions();
      }
   }

   public interface TestInterface {
      void DoSomething();
   }
}
