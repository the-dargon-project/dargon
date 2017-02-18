using System;

namespace NMockito.Fluent {
   public static class FluentAssertionStatics {
      private static NMockitoCoreImpl Core => NMockitoCoreImpl.Instance;

      public static void IsEqualTo(this byte self, byte value) {
         Core.AssertEquals(self, value);
      }

      public static void IsEqualTo(this sbyte self, sbyte value) {
         Core.AssertEquals(self, value);
      }

      public static void IsEqualTo(this ushort self, ushort value) {
         Core.AssertEquals(self, value);
      }

      public static void IsEqualTo(this short self, short value) {
         Core.AssertEquals(self, value);
      }

      public static void IsEqualTo<T>(this T self, T value) {
         Core.AssertEquals(self, value);
      }

      public static void IsTrue(this bool self) {
         Core.AssertTrue(self);
      }

      public static void IsFalse(this bool self) {
         Core.AssertFalse(self);
      }

      public static void Throws<TException>(this object returnValue) where TException : Exception {
         Core.AssertThrown<TException>();
      }
   }
}
