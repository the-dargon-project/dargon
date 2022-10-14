using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dargon.Commons {
   public static class Interlocked2 {
      [MethodImpl(MethodImplOptions.AggressiveInlining)] public static int Read(ref int x) => Interlocked.CompareExchange(ref x, 0, 0);
      [MethodImpl(MethodImplOptions.AggressiveInlining)] public static uint Read(ref uint x) => Interlocked.CompareExchange(ref x, 0U, 0U);
      [MethodImpl(MethodImplOptions.AggressiveInlining)] public static float Read(ref float x) => Interlocked.CompareExchange(ref x, 0.0f, 0.0f);
      [MethodImpl(MethodImplOptions.AggressiveInlining)] public static double Read(ref double x) => Interlocked.CompareExchange(ref x, 0.0, 0.0);
      [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T Read<T>(ref T x) where T : class => Interlocked.CompareExchange(ref x, null, null);

      [MethodImpl(MethodImplOptions.AggressiveInlining)] public static int PreIncrement(ref int x) => Interlocked.Increment(ref x);
      [MethodImpl(MethodImplOptions.AggressiveInlining)] public static uint PreIncrement(ref uint x) => Interlocked.Increment(ref x);
      [MethodImpl(MethodImplOptions.AggressiveInlining)] public static long PreIncrement(ref long x) => Interlocked.Increment(ref x);
      [MethodImpl(MethodImplOptions.AggressiveInlining)] public static ulong PreIncrement(ref ulong x) => Interlocked.Increment(ref x);

      [MethodImpl(MethodImplOptions.AggressiveInlining)] public static int PostIncrement(ref int x) => Interlocked.Increment(ref x) - 1;
      [MethodImpl(MethodImplOptions.AggressiveInlining)] public static uint PostIncrement(ref uint x) => Interlocked.Increment(ref x) - 1;
      [MethodImpl(MethodImplOptions.AggressiveInlining)] public static long PostIncrement(ref long x) => Interlocked.Increment(ref x) - 1;
      [MethodImpl(MethodImplOptions.AggressiveInlining)] public static ulong PostIncrement(ref ulong x) => Interlocked.Increment(ref x) - 1;

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static int WriteOrThrow(ref int x, int val) {
         var xCapture = Read(ref x);
         return Interlocked.CompareExchange(ref x, val, xCapture).AssertEquals(xCapture);
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static uint WriteOrThrow(ref uint x, uint val) {
         var xCapture = Read(ref x);
         return Interlocked.CompareExchange(ref x, val, xCapture).AssertEquals(xCapture);
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static float WriteOrThrow(ref float x, float val) {
         var xCapture = Read(ref x);
         return Interlocked.CompareExchange(ref x, val, xCapture).AssertEquals(xCapture);
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static double WriteOrThrow(ref double x, double val) {
         var xCapture = Read(ref x);
         return Interlocked.CompareExchange(ref x, val, xCapture).AssertEquals(xCapture);
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static T WriteOrThrow<T>(ref T x, T val) where T : class {
         var xCapture = Read(ref x);
         return Interlocked.CompareExchange(ref x, val, xCapture).AssertReferenceEquals(xCapture);
      }
      
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static void Write(ref uint x, uint val) {
         while (true) {
            var read = Read(ref x);
            if (read == val) return;
            if (Interlocked.CompareExchange(ref x, val, read) == read) return;
         }
      }
   }
}
