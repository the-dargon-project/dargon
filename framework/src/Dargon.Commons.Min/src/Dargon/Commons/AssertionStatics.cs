using System.Collections.Generic;
using System.Numerics;

namespace Dargon.Commons {
   public static class AssertionStatics {
      public static bool AssertIsTrue(this bool val, string message = null) {
         Assert.IsTrue(val, message);
         return val;
      }

      public static bool AssertIsFalse(this bool val, string message = null) {
         Assert.IsFalse(val, message);
         return val;
      }

      public static T? AssertIsNull<T>(this T val, string message = null) {
         Assert.IsNull(val, message);
         return val;
      }

#nullable enable

      public static T AssertIsNotNull<T>(this T? val, string? message = null) where T : class {
         Assert.IsNotNull(val, message);
         return val!;
      }

      public static T AssertIsNotNull<T>(this T? val, string? message = null) where T : struct {
         Assert.IsNotNull(val, message);
         return val!.Value;
      }

#nullable disable

      public static T AssertEquals<T>(this T actual, T expected) {
         Assert.Equals(expected, actual);
         return actual;
      }

      public static T AssertNotEquals<T>(this T val, T actual) {
         Assert.NotEquals(val, actual);
         return val;
      }

      public static T AssertReferenceEquals<T>(this T actual, T expected) where T : class {
         Assert.ReferenceEquals(expected, actual);
         return actual;
      }

      public static T AssertReferenceNotEquals<T>(this T val, T actual) where T : class {
         Assert.ReferenceNotEquals(val, actual);
         return val;
      }

      public static float AssertIsLessThan(this float left, float right) {
         Assert.IsLessThan(left, right);
         return left;
      }

      public static double AssertIsLessThan(this double left, double right) {
         Assert.IsLessThan(left, right);
         return left;
      }

      public static int AssertIsLessThan(this int left, int right) {
         Assert.IsLessThan(left, right);
         return left;
      }

      public static float AssertIsLessThanOrEqualTo(this float left, float right) {
         Assert.IsLessThanOrEqualTo(left, right);
         return left;
      }

      public static double AssertIsLessThanOrEqualTo(this double left, double right) {
         Assert.IsLessThanOrEqualTo(left, right);
         return left;
      }

      public static int AssertIsLessThanOrEqualTo(this int left, int right) {
         Assert.IsLessThanOrEqualTo(left, right);
         return left;
      }

      public static uint AssertIsLessThanOrEqualTo(this uint left, uint right) {
         Assert.IsLessThanOrEqualTo(left, right);
         return left;
      }

      public static long AssertIsLessThanOrEqualTo(this long left, long right) {
         Assert.IsLessThanOrEqualTo(left, right);
         return left;
      }

      public static ulong AssertIsLessThanOrEqualTo(this ulong left, ulong right) {
         Assert.IsLessThanOrEqualTo(left, right);
         return left;
      }

      public static float AssertIsGreaterThan(this float left, float right) {
         Assert.IsGreaterThan(left, right);
         return left;
      }

      public static double AssertIsGreaterThan(this double left, double right) {
         Assert.IsGreaterThan(left, right);
         return left;
      }

      public static int AssertIsGreaterThan(this int left, int right) {
         Assert.IsGreaterThan(left, right);
         return left;
      }

      public static uint AssertIsGreaterThan(this uint left, uint right) {
         Assert.IsGreaterThan(left, right);
         return left;
      }

      public static long AssertIsGreaterThan(this long left, long right) {
         Assert.IsGreaterThan(left, right);
         return left;
      }

      public static ulong AssertIsGreaterThan(this ulong left, ulong right) {
         Assert.IsGreaterThan(left, right);
         return left;
      }

      public static float AssertIsGreaterThanOrEqualTo(this float left, float right) {
         Assert.IsGreaterThanOrEqualTo(left, right);
         return left;
      }

      public static double AssertIsGreaterThanOrEqualTo(this double left, double right) {
         Assert.IsGreaterThanOrEqualTo(left, right);
         return left;
      }

      public static int AssertIsGreaterThanOrEqualTo(this int left, int right) {
         Assert.IsGreaterThanOrEqualTo(left, right);
         return left;
      }

      public static uint AssertIsGreaterThanOrEqualTo(this uint left, uint right) {
         Assert.IsGreaterThanOrEqualTo(left, right);
         return left;
      }

      public static long AssertIsGreaterThanOrEqualTo(this long left, long right) {
         Assert.IsGreaterThanOrEqualTo(left, right);
         return left;
      }

      public static ulong AssertIsGreaterThanOrEqualTo(this ulong left, ulong right) {
         Assert.IsGreaterThanOrEqualTo(left, right);
         return left;
      }

      public static int AssertIsWithinEpsilon(this int val, int epsilon) {
         Assert.IsWithinEpsilon(val, epsilon);
         return val;
      }

      public static float AssertIsWithinEpsilon(this float val, float epsilon) {
         Assert.IsWithinEpsilon(val, epsilon);
         return val;
      }

      public static double AssertIsWithinEpsilon(this double val, double epsilon) {
         Assert.IsWithinEpsilon(val, epsilon);
         return val;
      }

      public static int AssertIsWithinEpsilon(this int left, int right, int epsilon) {
         Assert.IsWithinEpsilon(left, right, epsilon);
         return left;
      }

      public static float AssertIsWithinEpsilon(this float left, float right, float epsilon) {
         Assert.IsWithinEpsilon(left, right, epsilon);
         return left;
      }

      public static double AssertIsWithinEpsilon(this double left, double right, double epsilon) {
         Assert.IsWithinEpsilon(left, right, epsilon);
         return left;
      }

      public static float AssertIsNumber(this float val, string message = null) {
         Assert.IsTrue(!float.IsNaN(val), "Value was NaN");
         Assert.IsTrue(!float.IsPositiveInfinity(val), "Value was +Infinity");
         Assert.IsTrue(!float.IsNegativeInfinity(val), "Value was -Infinity");
         return val;
      }

      public static double AssertIsNumber(this double val, string message = null) {
         Assert.IsTrue(!double.IsNaN(val), "Value was NaN");
         Assert.IsTrue(!double.IsPositiveInfinity(val), "Value was +Infinity");
         Assert.IsTrue(!double.IsNegativeInfinity(val), "Value was -Infinity");
         return val;
      }

      public static T[] AssertLengthEquals<T>(this T[] actual, int expected) {
         Assert.Equals(expected, actual.Length);
         return actual;
      }

      public static List<T> AssertLengthEquals<T>(this List<T> actual, int expected) {
         Assert.Equals(actual.Count, expected);
         return actual;
      }

      public static T AssertHasFlags<T>(this T value, T flag) where T : struct, System.Enum {
         Assert.HasFlags(value, flag);
         return value;
      }

      public static T AssertHasUnsetFlags<T>(this T value, T flag) where T : struct, System.Enum {
         Assert.HasUnsetFlags(value, flag);
         return value;
      }
   }
}