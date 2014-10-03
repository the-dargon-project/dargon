using System;

namespace NMockito
{
   internal class VerificationTimesMismatchException : Exception
   {
      public VerificationTimesMismatchException(int expected, int actual, Exception innerException = null, string explanation = null)
         : base(("Expected " + expected + " invocations but found " + actual + " invocations") + (explanation == null ? "" : "; " + explanation), innerException) { }
   }

   internal class VerificationNotInvokedException : Exception
   {
      public VerificationNotInvokedException() : base("Did not find method invocation with given parameters") { }
   }
}