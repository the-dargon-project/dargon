using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dargon.Commons {
   public static class Interlocked2 {
      public static int Read(ref int x) => Interlocked.CompareExchange(ref x, 0, 0);
      public static uint Read(ref uint x) => Interlocked.CompareExchange(ref x, 0U, 0U);
      public static float Read(ref float x) => Interlocked.CompareExchange(ref x, 0.0f, 0.0f);
      public static double Read(ref double x) => Interlocked.CompareExchange(ref x, 0.0, 0.0);
      public static T Read<T>(ref T x) where T : class => Interlocked.CompareExchange(ref x, null, null);

      public static int WriteOrThrow(ref int x, int val) {
         var xCapture = x;
         return Interlocked.CompareExchange(ref x, val, xCapture).AssertEquals(xCapture);
      }

      public static uint WriteOrThrow(ref uint x, uint val) {
         var xCapture = x;
         return Interlocked.CompareExchange(ref x, val, xCapture).AssertEquals(xCapture);
      }

      public static float WriteOrThrow(ref float x, float val) {
         var xCapture = x;
         return Interlocked.CompareExchange(ref x, val, xCapture).AssertEquals(xCapture);
      }

      public static double WriteOrThrow(ref double x, double val) {
         var xCapture = x;
         return Interlocked.CompareExchange(ref x, val, xCapture).AssertEquals(xCapture);
      }

      public static T WriteOrThrow<T>(ref T x, T val) where T : class {
         var xCapture = x;
         return Interlocked.CompareExchange(ref x, val, xCapture).AssertReferenceEquals(xCapture);
      }

      public static void Write(ref uint x, uint val) {
         while (true) {
            var read = Read(ref x);
            if (read == val) return;
            if (Interlocked.CompareExchange(ref x, val, read) == read) return;
         }
      }
   }
}
