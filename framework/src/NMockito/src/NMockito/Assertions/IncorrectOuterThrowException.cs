using System;

namespace NMockito.Assertions {
   public class IncorrectOuterThrowException : Exception {
      public IncorrectOuterThrowException(
         Type expected, 
         Exception received
      ) : base(GetMessage(expected, received), received) { }

      private static string GetMessage(Type expected, Exception received) {
         return $"Expected exception of type {expected.FullName} but got {received.GetType().FullName}.";
      }
   }
}
