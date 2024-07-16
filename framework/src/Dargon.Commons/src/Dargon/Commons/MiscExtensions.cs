using System;

namespace Dargon.Commons {
   public static class MiscExtensions {
      public static string ToHexStringLower(this Guid g, int bytesIncluded = 16) {
         Span<byte> buf = stackalloc byte[16];
         g.TryWriteBytes(buf).AssertIsTrue();

         return Convert.ToHexStringLower(buf[0..bytesIncluded]);
      }

      public static string ToShortHexStringLower(this Guid g) => g.ToHexStringLower(8);

      public static string ToHexStringUpper(this Guid g, int bytesIncluded = 16) {
         Span<byte> buf = stackalloc byte[16];
         g.TryWriteBytes(buf).AssertIsTrue();

         return Convert.ToHexString(buf[0..bytesIncluded]);
      }

      public static string ToShortHexStringUpper(this Guid g) => g.ToHexStringUpper(8);

      public static string ToHexString(this Guid g, int bytesIncluded = 16) => ToHexStringLower(g, bytesIncluded);

      public static string ToShortHexString(this Guid g) => g.ToShortHexStringLower();
   }
}
