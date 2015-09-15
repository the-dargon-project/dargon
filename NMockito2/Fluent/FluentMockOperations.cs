using System;
using NMockito2.Expectations;

namespace NMockito2.Fluent {
   public static class FluentMockOperations {
      public static FluentExpectation<T> Returns<T>(this T self, T value) {
         return new FluentExpectation<T>(NMockitoInstance.Instance.Expect<T>(default(T)).ThenReturn(value));
      }

      public static FluentExpectation<T> Throws<T>(this T self, Exception exception) {
         return new FluentExpectation<T>(NMockitoInstance.Instance.Expect<T>(default(T)).ThenThrow(exception));
      }
   }

   public class FluentExpectation<T> {
      private readonly Expectation<T> expectation;

      public FluentExpectation(Expectation<T> expectation) {
         this.expectation = expectation;
      }

      public FluentExpectation<T> ThenReturns(T value) {
         expectation.ThenReturn(value);
         return this;
      }

      public FluentExpectation<T> ThenThrows(Exception e) {
         expectation.ThenThrow(e);
         return this;
      }
   }
}
