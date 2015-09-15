using System;

namespace NMockito2.Fluent {
   public static class FluentMockOperationStatics {
      public static FluentExpectation<T> Returns<T>(this T self, params T[] values) {
         return new FluentExpectation<T>(NMockitoInstance.Instance.Expect<T>(default(T)).ThenReturn(values));
      }

      public static FluentExpectation<T> Throws<T>(this T self, params Exception[] exceptions) {
         return new FluentExpectation<T>(NMockitoInstance.Instance.Expect<T>(default(T)).ThenThrow(exceptions));
      }
   }
}
