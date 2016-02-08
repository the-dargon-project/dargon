using NMockito.BehavioralTesters;
using Xunit;

namespace NMockito {
   public class NMockitoStaticsTests : NMockitoInstance {
      [Fact]
      public void NMockitoStatics_DelegatesTo_NMockitoInstance() {
         var tester = new StaticProxyBehaviorTester(this);
         tester.TestStaticProxy(typeof(NMockitoStatics));
      } 
   }
}