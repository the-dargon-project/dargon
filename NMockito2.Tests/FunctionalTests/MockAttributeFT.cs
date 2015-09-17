using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NMockito2.Attributes;
using NMockito2.Fluent;
using Xunit;

namespace NMockito2.FunctionalTests {
   public class MockAttributeFT : NMockitoInstance {
      [Mock] private readonly TestInterface testInterface = null;

      [Fact]
      public void InterfaceFields_AreInitializedWithMocks() {
         testInterface.Run("asdf").Returns(10);
         testInterface.Run("asdf").IsEqualTo(10);
      }

      internal interface TestInterface {
         int Run(string x);
      }
   }
}
