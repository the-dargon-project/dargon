using NMockito;
using Xunit;

namespace Dargon.Ryu {
   public class RyuContainerImplIT : NMockitoInstance {
      private readonly IRyuFacade testObj;

      public RyuContainerImplIT() {
         testObj = new RyuFactory().Create();
      }

      [Fact]
      public void Get_RyuContainer_ReturnsSelfTest() {
         AssertEquals(testObj, testObj.GetOrThrow<IRyuFacade>());
         AssertEquals(testObj, testObj.GetOrThrow<RyuFacade>());
      }
   }
}