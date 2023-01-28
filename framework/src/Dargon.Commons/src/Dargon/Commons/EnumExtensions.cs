using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Dargon.Commons {
   public static class EnumExtensions {
      static EnumExtensions() {
         Unsafe.SizeOf<byte>().AssertEquals(1);
         Unsafe.SizeOf<short>().AssertEquals(2);
         Unsafe.SizeOf<int>().AssertEquals(4);
         Unsafe.SizeOf<long>().AssertEquals(8);

         (BindingFlags.CreateInstance | BindingFlags.IgnoreCase).FastHasFlag(BindingFlags.IgnoreCase).AssertIsTrue();
         (BindingFlags.CreateInstance).FastHasFlag(BindingFlags.IgnoreCase).AssertIsFalse();
      }

      public static long ToInt64<T>(this T val) where T : struct, Enum {
         var szT = Unsafe.SizeOf<T>();
         if (szT == 1) {
            return Unsafe.As<T, byte>(ref val);
         } else if (szT == 2) {
            return Unsafe.As<T, short>(ref val);
         } else if (szT == 4) {
            return Unsafe.As<T, int>(ref val);
         } else if (szT == 8) {
            return Unsafe.As<T, long>(ref val);
         } else {
            throw new NotSupportedException();
         }
      }

      public static T ToEnum<T>(this long val) where T : struct, Enum {
         var szT = Unsafe.SizeOf<T>();
         if (szT == 1) {
            var v = (byte)val;
            return Unsafe.As<byte, T>(ref v);
         } else if (szT == 2) {
            var v = (short)val;
            return Unsafe.As<short, T>(ref v);
         } else if (szT == 4) {
            var v = (int)val;
            return Unsafe.As<int, T>(ref v);
         } else if (szT == 8) {
            var v = val;
            return Unsafe.As<long, T>(ref v);
         } else {
            throw new NotSupportedException();
         }
      }

      public static bool FastHasFlag<T>(this T val, T flag) where T : struct, Enum {
         var szT = Unsafe.SizeOf<T>();
         if (szT == 1) {
            var v = Unsafe.As<T, byte>(ref val);
            var f = Unsafe.As<T, byte>(ref flag);
            return (v & f) == f;
         } else if (szT == 2) {
            var v = Unsafe.As<T, short>(ref val);
            var f = Unsafe.As<T, short>(ref flag);
            return (v & f) == f;
         } else if (szT == 4) {
            var v = Unsafe.As<T, int>(ref val);
            var f = Unsafe.As<T, int>(ref flag);
            return (v & f) == f;
         } else if (szT == 8) {
            var v = Unsafe.As<T, long>(ref val);
            var f = Unsafe.As<T, long>(ref flag);
            return (v & f) == f;
         } else {
            throw new NotSupportedException();
         }
      }

      public static bool FastHasAnyFlag<T>(this T val, T flags) where T : struct, Enum {
         var szT = Unsafe.SizeOf<T>();
         if (szT == 1) {
            var v = Unsafe.As<T, byte>(ref val);
            var f = Unsafe.As<T, byte>(ref flags);
            return (v & f) == f;
         } else if (szT == 2) {
            var v = Unsafe.As<T, short>(ref val);
            var f = Unsafe.As<T, short>(ref flags);
            return (v & f) == f;
         } else if (szT == 4) {
            var v = Unsafe.As<T, int>(ref val);
            var f = Unsafe.As<T, int>(ref flags);
            return (v & f) == f;
         } else if (szT == 8) {
            var v = Unsafe.As<T, long>(ref val);
            var f = Unsafe.As<T, long>(ref flags);
            return (v & f) == f;
         } else {
            throw new NotSupportedException();
         }
      }

      public static string ToBinaryString(this int v)
         => Convert.ToString(v, 2);

      public static string ToBinaryString(this long v)
         => Convert.ToString(v, 2);

      public static string ToBinaryString<T>(this T v) where T : Enum
         => Convert.ToString(Convert.ToInt64(v), 2);
   }
}
