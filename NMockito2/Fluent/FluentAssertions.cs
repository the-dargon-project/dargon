using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMockito2.Fluent {
   public static class FluentAssertions {
      private static NMockitoInstance Instance => NMockitoInstance.Instance;

      public static void IsEqualTo<T>(this T self, T value) {
         Instance.AssertEquals(self, value);
      }

      public static void IsTrue(this bool self) {
         Instance.AssertTrue(self);
      }

      public static void IsFalse(this bool self) {
         Instance.AssertFalse(self);
      }
   }
}
