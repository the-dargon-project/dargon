using System;
using System.Diagnostics;

namespace Dargon.Commons {
   public static class Assert {
      public static void IsTrue(bool val, string message = null) {
         if (!val) {
            Fail(message ?? "(Value not true)");
         }
      }

      public static void IsFalse(bool val, string message = null) {
         if (val) {
            Fail(message ?? "(Value not false)");
         }
      }

      public static void IsNull(object val, string message = null) {
         if (val != null) {
            Fail(message ?? "(Value not null)");
         }
      }

      public static void IsNotNull(object val, string message = null) {
         if (val == null) {
            Fail(message ?? "(Value was null)");
         }
      }

      public static void Equals<T>(T expected, T actual) {
         if (!Object.Equals(expected, actual)) {
            Fail($"AssertEquals failed. Expected: {expected}, Actual: {actual}");
         }
      }

      public static void Fail(string message) {
         Debugger.Break();

         Console.Error.WriteLine("Assertion Failure: " + message);
         Console.Error.WriteLine(Environment.StackTrace);
         Console.Error.Flush();

#if DEBUG
         Debug.Assert(false, message);
#elif TRACE
         Trace.Assert(false, message);
#else
         // We don't throw as that could be caught by a catch.
#error Trace/Debug not defined so assertions cannot fail.
#endif

         // welp, if we get here that's because Debug/Trace asserts are getting caught (e.g. by Unity). Throw.
         throw new AssertionFailureException(message);
      }

      private class AssertionFailureException : Exception {
         public AssertionFailureException(string message) : base(message) { }
      }
   }
}
