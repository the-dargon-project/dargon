using NMockito.Attributes;
using NMockito.Fluent;
using Xunit;

namespace NMockito.FunctionalTests {
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
