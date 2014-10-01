using System;

namespace ItzWarty.Test
{
   internal class VerificationTimesMismatchException : Exception
   {
      public VerificationTimesMismatchException(int expected, int actual)
         : base("Expected " + expected + " invocations but found " + actual + " invocations") { }
   }

   internal class VerificationNotInvokedException : Exception
   {
      public VerificationNotInvokedException() : base("Did not find method invocation with given parameters") { }
   }
}