using System;
using NMockito2.Expectations;

namespace NMockito2.Fluent {
   public static class FluentMockOperations {
      public static FluentExpectation<T> Returns<T>(this T self, params T[] values) {
         return new FluentExpectation<T>(NMockitoInstance.Instance.Expect<T>(default(T)).ThenReturn(values));
      }

      public static FluentExpectation<T> Throws<T>(this T self, params Exception[] exceptions) {
         return new FluentExpectation<T>(NMockitoInstance.Instance.Expect<T>(default(T)).ThenThrow(exceptions));
      }
   }

   public class FluentExpectation<T> {
      private readonly Expectation<T> expectation;

      public FluentExpectation(Expectation<T> expectation) {
         this.expectation = expectation;
      }

      public FluentExpectation<T> ThenReturns(params T[] value) {
         expectation.ThenReturn(value);
         return this;
      }

      public FluentExpectation<T> ThenThrows(params Exception[] e) {
         expectation.ThenThrow(e);
         return this;
      }
   }
}
