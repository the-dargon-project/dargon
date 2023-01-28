using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Dargon.Commons.Comparers;
using Dargon.Commons.Exceptions;

namespace Dargon.Commons {
   public static class Assert {
      [ThreadStatic] private static int assertionOutputSuppressionCounter;
      [ThreadStatic] private static int assertionBreakpointSuppressionCounter;

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

      public static void IsNull<T>(T val, string message = null) {
         if (val != null) {
            Fail(message ?? "(Value not null)");
         }
      }

      public static void IsNotNull<T>(T val, string message = null) {
         if (val == null) {
            Fail(message ?? "(Value was null)");
         }
      }

      public static void Equals<T>(T expected, T actual) {
         if (!EqualityComparer<T>.Default.Equals(expected, actual)) {
            Fail($"AssertEquals failed. Expected: {expected}, Actual: {actual}");
         }
      }

      public static void NotEquals<T>(T val, T actual) {
         if (EqualityComparer<T>.Default.Equals(val, actual)) {
            Fail($"AssertNotEquals failed. Val: {val}, Actual: {actual}");
         }
      }

      public static void ReferenceEquals<T>(T expected, T actual) {
         if (!object.ReferenceEquals(expected, actual)) {
            Fail($"AssertReferenceEquals failed. Expected: {expected}, Actual: {actual}");
         }
      }

      public static void ReferenceNotEquals<T>(T val, T actual) {
         if (object.ReferenceEquals(val, actual)) {
            Fail($"AssertReferenceNotEquals failed. Val: {val}, Actual: {actual}");
         }
      }

      public static void IsLessThan(float left, float right) {
         if (left >= right) {
            Fail($"{nameof(IsLessThan)} failed. {left} >= {right}");
         }
      }

      public static void IsLessThan(double left, double right) {
         if (left >= right) {
            Fail($"{nameof(IsLessThan)} failed. {left} >= {right}");
         }
      }

      public static void IsLessThan(int left, int right) {
         if (left >= right) {
            Fail($"{nameof(IsLessThan)} failed. {left} >= {right}");
         }
      }

      public static void IsLessThan(uint left, uint right) {
         if (left >= right) {
            Fail($"{nameof(IsLessThan)} failed. {left} >= {right}");
         }
      }

      public static void IsLessThan(long left, long right) {
         if (left >= right) {
            Fail($"{nameof(IsLessThan)} failed. {left} >= {right}");
         }
      }

      public static void IsLessThan(ulong left, ulong right) {
         if (left >= right) {
            Fail($"{nameof(IsLessThan)} failed. {left} >= {right}");
         }
      }

      public static void IsLessThanOrEqualTo(float left, float right) {
         if (left > right) {
            Fail($"{nameof(IsLessThanOrEqualTo)} failed. {left} > {right}");
         }
      }

      public static void IsLessThanOrEqualTo(double left, double right) {
         if (left > right) {
            Fail($"{nameof(IsLessThanOrEqualTo)} failed. {left} > {right}");
         }
      }

      public static void IsLessThanOrEqualTo(int left, int right) {
         if (left > right) {
            Fail($"{nameof(IsLessThanOrEqualTo)} failed. {left} > {right}");
         }
      }

      public static void IsLessThanOrEqualTo(uint left, uint right) {
         if (left > right) {
            Fail($"{nameof(IsLessThanOrEqualTo)} failed. {left} > {right}");
         }
      }

      public static void IsLessThanOrEqualTo(long left, long right) {
         if (left > right) {
            Fail($"{nameof(IsLessThanOrEqualTo)} failed. {left} > {right}");
         }
      }

      public static void IsLessThanOrEqualTo(ulong left, ulong right) {
         if (left > right) {
            Fail($"{nameof(IsLessThanOrEqualTo)} failed. {left} > {right}");
         }
      }

      public static void IsGreaterThan(float left, float right) {
         if (left <= right) {
            Fail($"{nameof(IsGreaterThan)} failed. {left} <= {right}");
         }
      }

      public static void IsGreaterThan(double left, double right) {
         if (left <= right) {
            Fail($"{nameof(IsGreaterThan)} failed. {left} <= {right}");
         }
      }

      public static void IsGreaterThan(int left, int right) {
         if (left <= right) {
            Fail($"{nameof(IsGreaterThan)} failed. {left} <= {right}");
         }
      }

      public static void IsGreaterThan(uint left, uint right) {
         if (left <= right) {
            Fail($"{nameof(IsGreaterThan)} failed. {left} <= {right}");
         }
      }

      public static void IsGreaterThan(long left, long right) {
         if (left <= right) {
            Fail($"{nameof(IsGreaterThan)} failed. {left} <= {right}");
         }
      }

      public static void IsGreaterThan(ulong left, ulong right) {
         if (left <= right) {
            Fail($"{nameof(IsGreaterThan)} failed. {left} <= {right}");
         }
      }

      public static void IsGreaterThanOrEqualTo(float left, float right) {
         if (left < right) {
            Fail($"{nameof(IsGreaterThanOrEqualTo)} failed. {left} < {right}");
         }
      }

      public static void IsGreaterThanOrEqualTo(double left, double right) {
         if (left < right) {
            Fail($"{nameof(IsGreaterThanOrEqualTo)} failed. {left} < {right}");
         }
      }

      public static void IsGreaterThanOrEqualTo(int left, int right) {
         if (left < right) {
            Fail($"{nameof(IsGreaterThanOrEqualTo)} failed. {left} < {right}");
         }
      }

      public static void IsGreaterThanOrEqualTo(uint left, uint right) {
         if (left < right) {
            Fail($"{nameof(IsGreaterThanOrEqualTo)} failed. {left} < {right}");
         }
      }

      public static void IsGreaterThanOrEqualTo(long left, long right) {
         if (left < right) {
            Fail($"{nameof(IsGreaterThanOrEqualTo)} failed. {left} < {right}");
         }
      }

      public static void IsGreaterThanOrEqualTo(ulong left, ulong right) {
         if (left < right) {
            Fail($"{nameof(IsGreaterThanOrEqualTo)} failed. {left} < {right}");
         }
      }

      public static void IsWithinEpsilon(int a, int b, int epsilon) {
         var delta = Math.Abs(a - b);
         if (delta > epsilon) {
            Fail($"{nameof(IsWithinEpsilon)} failed. abs({a} - {b}) = {delta}, which is > {epsilon}");
         }
      }

      public static void IsWithinEpsilon(float a, float b, float epsilon) {
         var delta = Math.Abs(a - b);
         if (delta > epsilon) {
            Fail($"{nameof(IsWithinEpsilon)} failed. abs({a} - {b}) = {delta}, which is > {epsilon}");
         }
      }

      public static void IsWithinEpsilon(double a, double b, double epsilon) {
         var delta = Math.Abs(a - b);
         if (delta > epsilon) {
            Fail($"{nameof(IsWithinEpsilon)} failed. abs({a} - {b}) = {delta}, which is > {epsilon}");
         }
      }

      public static void IsWithinEpsilon(int v, int epsilon) {
         if (Math.Abs(v) > epsilon) {
            Fail($"{nameof(IsWithinEpsilon)} failed. abs({v}) > {epsilon}");
         }
      }

      public static void IsWithinEpsilon(float v, float epsilon) {
         if (Math.Abs(v) > epsilon) {
            Fail($"{nameof(IsWithinEpsilon)} failed. abs({v}) > {epsilon}");
         }
      }

      public static void IsWithinEpsilon(double v, double epsilon) {
         if (Math.Abs(v) > epsilon) {
            Fail($"{nameof(IsWithinEpsilon)} failed. abs({v}) > {epsilon}");
         }
      }

      public static void HasFlags<T>(T value, T flags) where T : struct, Enum {
         var v = value.ToInt64();
         var f = flags.ToInt64();
         var vAndF = v & f;
         if (vAndF != f) {
            Fail($"{nameof(HasFlags)} failed. {value} ({v.ToBinaryString()}) & {flags} {f.ToBinaryString()} = {vAndF.ToEnum<T>()} ({vAndF.ToBinaryString()}");
         }
      }

      public static void HasUnsetFlags<T>(T value, T flags) where T : struct, Enum {
         var v = value.ToInt64();
         var f = flags.ToInt64();
         var vAndF = v & f;
         if (vAndF != 0) {
            Fail($"{nameof(HasUnsetFlags)} failed. {value} ({v.ToBinaryString()}) & {flags} {f.ToBinaryString()} = {vAndF.ToEnum<T>()} ({vAndF.ToBinaryString()}");
         }
      }

      public static Exception Fail(string message) {
         var testAssemblyNames = new[] {
            "Microsoft.VisualStudio.QualityTools.UnitTestFramework",
            "xunit.runner"
         };
         
         var isRunningInTest = AppDomain.CurrentDomain.GetAssemblies()
                                        .Any(a => testAssemblyNames.Any(a.FullName.Contains));

         if (assertionBreakpointSuppressionCounter == 0 && !isRunningInTest && Debugger.IsAttached) {
            // undefined behavior in test runner (e.g. some test runners/profilers actually
            // attach a debugger)
            Debugger.Break();
         }

         if (assertionOutputSuppressionCounter == 0) {
            Console.Out.Flush();
            Console.Error.WriteLine("Assertion Failure: " + message);
            Console.Error.WriteLine(Environment.StackTrace);
            Console.Error.Flush();
         }

         // asserts crash test runner, as opposed to failing test.
         if (assertionBreakpointSuppressionCounter == 0 && !isRunningInTest && Debugger.IsAttached) {
#if DEBUG
            Debug.Assert(false, message);
#elif TRACE
         Trace.Assert(false, message);
#else
         // We don't throw as that could be caught by a catch.
#error Trace/Debug not defined so assertions cannot fail.
#endif
         }

         // So you can no-op the fail in debugger by moving instruction pointer.
         if (DateTime.Now == default) {
            return new NotImplementedException();
         }

         // welp, if we get here that's because Debug/Trace asserts are getting caught (e.g. by Unity). Throw.
         throw new AssertionFailureException(message);
      }

      public static ThreadAssertionFailureSuppressionSession OpenFailureLogAndBreakpointSuppressionBlock(bool suppressLog = true, bool suppressBreakpoint = true) {
         return new ThreadAssertionFailureSuppressionSession(suppressLog, suppressBreakpoint);
      }

      public class ThreadAssertionFailureSuppressionSession : IDisposable {
         private readonly bool suppressLog;
         private readonly bool suppressBreakpoint;

         public ThreadAssertionFailureSuppressionSession(bool suppressLog, bool suppressBreakpoint) {
            this.suppressLog = suppressLog;
            this.suppressBreakpoint = suppressBreakpoint;

            if (suppressLog) assertionOutputSuppressionCounter++;
            if (suppressBreakpoint) assertionBreakpointSuppressionCounter++;
         }

         public void Dispose() {
            CleanupInternal();
            GC.SuppressFinalize(this);
         }

         ~ThreadAssertionFailureSuppressionSession() {
            CleanupInternal();
            throw new InvalidStateException($"{nameof(ThreadAssertionFailureSuppressionSession)} finalizer invoked - did dispose get called?");
         }

         private void CleanupInternal() {
            if (suppressLog) assertionOutputSuppressionCounter--;
            if (suppressBreakpoint) assertionBreakpointSuppressionCounter--;
         }
      }
   }
   public class AssertionFailureException : Exception {
      public AssertionFailureException(string message) : base(message) { }
   }
}
