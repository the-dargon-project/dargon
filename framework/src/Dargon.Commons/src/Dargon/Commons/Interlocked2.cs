﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dargon.Commons {
   public static class DebugCounters {
      public static void Increment(ref int i) {
#if DEBUG
         Interlocked.Increment(ref i);
#endif
      }

      public static void Add(ref int i, int count) {
#if DEBUG
         Interlocked.Add(ref i, count);
#endif
      }
   }

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

      [MethodImpl(MethodImplOptions.AggressiveInlining)] public static int PreDecrement(ref int x) => Interlocked.Decrement(ref x);
      [MethodImpl(MethodImplOptions.AggressiveInlining)] public static uint PreDecrement(ref uint x) => Interlocked.Decrement(ref x);
      [MethodImpl(MethodImplOptions.AggressiveInlining)] public static long PreDecrement(ref long x) => Interlocked.Decrement(ref x);
      [MethodImpl(MethodImplOptions.AggressiveInlining)] public static ulong PreDecrement(ref ulong x) => Interlocked.Decrement(ref x);

      [MethodImpl(MethodImplOptions.AggressiveInlining)] public static int PostDecrement(ref int x) => Interlocked.Decrement(ref x) + 1;
      [MethodImpl(MethodImplOptions.AggressiveInlining)] public static uint PostDecrement(ref uint x) => Interlocked.Decrement(ref x) + 1;
      [MethodImpl(MethodImplOptions.AggressiveInlining)] public static long PostDecrement(ref long x) => Interlocked.Decrement(ref x) + 1;
      [MethodImpl(MethodImplOptions.AggressiveInlining)] public static ulong PostDecrement(ref ulong x) => Interlocked.Decrement(ref x) + 1;

      public static int PreIncrementWithMod(ref int x, int mod) {
         while (true) {
            var current = Interlocked2.Read(ref x);
            var next = current + 1 == mod ? 0 : current + 1;
            if (Interlocked.CompareExchange(ref x, next, current) == current) {
               return next;
            }
         }
      }

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
      public static void Write(ref int x, int val) {
         while (true) {
            var read = Read(ref x);
            if (read == val) return;
            if (Interlocked.CompareExchange(ref x, val, read) == read) return;
         }
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static void Write(ref uint x, uint val) {
         while (true) {
            var read = Read(ref x);
            if (read == val) return;
            if (Interlocked.CompareExchange(ref x, val, read) == read) return;
         }
      }


      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static void Write<T>(ref T o, T val) where T : class {
         while (true) {
            var read = Read(ref o);
            if (read == val) return;
            if (Interlocked.CompareExchange(ref o, val, read) == read) return;
         }
      }

#nullable enable
      public static T AssignIfNull<T>(ref T? target, T value) where T : class {
         return Interlocked.CompareExchange(ref target, value, null) ?? value;
      }
#nullable restore
   }
}
