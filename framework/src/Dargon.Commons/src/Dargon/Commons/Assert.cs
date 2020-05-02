﻿using System;
using System.Diagnostics;
using System.Linq;

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
         var testAssemblyNames = new[] {
            "Microsoft.VisualStudio.QualityTools.UnitTestFramework",
            "xunit.runner"
         };
         
         var isRunningInTest = AppDomain.CurrentDomain.GetAssemblies()
                                        .Any(a => testAssemblyNames.Any(a.FullName.Contains));

         if (!isRunningInTest) {
            // undefined behavior in test runner (e.g. some test runners/profilers actually
            // attach a debugger)
            Debugger.Break();
         }

         Console.Error.WriteLine("Assertion Failure: " + message);
         Console.Error.WriteLine(Environment.StackTrace);
         Console.Error.Flush();

         // asserts crash test runner, as opposed to failing test.
         if (!isRunningInTest) {
#if DEBUG
            Debug.Assert(false, message);
#elif TRACE
         Trace.Assert(false, message);
#else
         // We don't throw as that could be caught by a catch.
#error Trace/Debug not defined so assertions cannot fail.
#endif
         }

         // welp, if we get here that's because Debug/Trace asserts are getting caught (e.g. by Unity). Throw.
         throw new AssertionFailureException(message);
      }

      private class AssertionFailureException : Exception {
         public AssertionFailureException(string message) : base(message) { }
      }
   }
}
