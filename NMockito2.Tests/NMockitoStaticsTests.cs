using System.CodeDom;
using NMockito2.BehavioralTesters;
using Xunit;

namespace NMockito2 {
   public class NMockitoStaticsTests : NMockitoInstance {
      [Fact]
      public void NMockitoStatics_DelegatesTo_NMockitoInstance() {
         var tester = new StaticProxyBehaviorTester(this);
         tester.TestStaticProxy(typeof(NMockitoStatics));
      } 
   }
}