using System;

namespace NMockito.Fluent {
   public static class FluentMockOperationStatics {
      private static NMockitoCore Core => NMockitoCoreImpl.Instance;

      public static FluentExpectation<T> Returns<T>(this T self, params T[] values) {
         return new FluentExpectation<T>(Core.Expect<T>(default(T)).ThenReturn(values));
      }

      public static FluentExpectation<T> Throws<T>(this T self, params Exception[] exceptions) {
         return new FluentExpectation<T>(Core.Expect<T>(default(T)).ThenThrow(exceptions));
      }
   }
}
