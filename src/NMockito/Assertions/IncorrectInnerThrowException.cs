using System;

namespace NMockito.Assertions {
   public class IncorrectInnerThrowException : Exception {
      public IncorrectInnerThrowException(
         Type expected,
         Exception received
         ) : base(GetMessage(expected, received), received) { }

      private static string GetMessage(Type expected, Exception received) {
         return $"Expected but did not find inner exception type {expected.FullName}, instead got {received}.";
      }
   }
}