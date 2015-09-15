using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NMockito2.Fluent;
using Xunit;

namespace NMockito2.FunctionalTests {
   public class ParamsFT : NMockitoInstance {
      [Fact]
      public void Run() {
         var testObj = CreateMock<TestInterface>();
         testObj.Invoke(10, "a", "b").Returns("10ab");
         testObj.Invoke(20, null).Returns("20null");
         
         AssertEquals("10ab", testObj.Invoke(10, "a", "b"));
         AssertEquals("20null", testObj.Invoke(20, null));
      }

      internal interface TestInterface {
         string Invoke(int n, params string[] parameters);
      }
   }
}
