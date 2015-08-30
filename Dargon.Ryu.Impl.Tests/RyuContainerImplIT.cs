using NMockito;
using Xunit;

namespace Dargon.Ryu {
   public class RyuContainerImplIT : NMockitoInstance {
      private readonly RyuContainer testObj;

      public RyuContainerImplIT() {
         testObj = new RyuFactory().Create();
         testObj.Setup();
      }

      [Fact]
      public void Get_RyuContainer_ReturnsSelfTest() {
         AssertEquals(testObj, testObj.Get<RyuContainer>());
         AssertEquals(testObj, testObj.Get<RyuContainerImpl>());
      }
   }
}