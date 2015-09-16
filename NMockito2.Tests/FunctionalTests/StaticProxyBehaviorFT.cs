using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NMockito2.BehavioralTesters;
using Xunit;

namespace NMockito2.FunctionalTests {
   public class StaticProxyBehaviorFT : NMockitoInstance {
      [Fact]
      public void GoodStatic_ProxyTest() {
         new StaticProxyBehaviorTester(this).TestStaticProxy(typeof(GoodStatic));
      }

      [Fact]
      public void BadStatic_ProxyTest() {
         AssertThrows<EntryPointNotFoundException>(() => {
            new StaticProxyBehaviorTester(this).TestStaticProxy(typeof(BadStatic));
         });
      }

      internal interface Interface1 {
         int A<T>(string x, T y) where T : struct;
         int B(string x);
         bool C<T, U>(T x, out U y) where T : class where U : struct;
      }

      internal interface Interface2 {
//         bool C<T>(string x, out T y);
         bool D(string x, out int y);
      }

      internal static class GoodStatic {
         private static Interface1 interface1;
         private static Interface2 interface2;

         public static int A<T>(string x, T y) where T : struct => interface1.A(x, y);
         public static int B(string x) => interface1.B(x);
         public static bool C<T, U>(T x, out U y) where T : class where U : struct => interface1.C(x, out y);
         public static bool D(string x, out int y) => interface2.D(x, out y);
      }

      internal static class BadStatic {
         private static Interface1 interface1;

         public static int A<T>(string x, T y) where T : struct => interface1.A(x, y);
         public static int B(string x) => interface1.B(x);
      }
   }
}
