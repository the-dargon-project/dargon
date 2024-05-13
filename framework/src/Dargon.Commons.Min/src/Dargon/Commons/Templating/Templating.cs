using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dargon.Commons.Templating {
   public class TArg<T> {
      public static bool Is<U>() => typeof(T) == typeof(U);

      public static void AssertMatchesAny<A1>() {
         if (typeof(T) == typeof(A1)) return;
         throw new ArgumentException($"{typeof(T).FullName} is not one of {typeof(A1).FullName}");
      }

      public static void AssertMatchesAny<A1, A2>() {
         if (typeof(T) == typeof(A1)) return;
         if (typeof(T) == typeof(A2)) return;
         throw new ArgumentException($"{typeof(T).FullName} is not one of {typeof(A1).FullName} or {typeof(A2).FullName}");
      }

      public static void AssertMatchesAny<A1, A2, A3>() {
         if (typeof(T) == typeof(A1)) return;
         if (typeof(T) == typeof(A2)) return;
         if (typeof(T) == typeof(A3)) return;
         throw new ArgumentException($"{typeof(T).FullName} is not one of {typeof(A1).FullName} or {typeof(A2).FullName} or {typeof(A3).FullName}");
      }
   }

   public class TBool<ArgTrue, ArgFalse> {
      public static bool IsTrue<T>() {
         if (typeof(T) == typeof(ArgTrue)) return true;
         else if (typeof(T) == typeof(ArgFalse)) return false;
         else throw new ArgumentException($"{typeof(T).FullName} is not {typeof(ArgTrue).FullName} or {typeof(ArgFalse).FullName}");
      }

      public static bool IsFalse<T>() => !IsTrue<T>();
   }

   public class TBool : TBool<TTrue, TFalse> {}
   public struct TDummy { }
   public struct TTrue {}
   public struct TFalse { }

   public record struct TypeId(int Value);

   public static class TypeIds<TNamespace> {
      // ReSharper disable once StaticMemberInGenericType
      private static int next;

      public static TypeId Get<T>() => Inner<T>.Value;

      private static class Inner<T> {
         // ReSharper disable once StaticMemberInGenericType
         public static TypeId Value { get; } = new(InterlockedMin.PostIncrement(ref next));
      }
   }


   public class TypeCounter<TType> {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static int GetIndex<TKey>() => TypeIds<DummyNamespace>.Get<TKey>().Value;

      private struct DummyNamespace {}
   }

   public interface ITemplateNint {
      public nint Value { get; }
   }

   public interface ITemplateNUint {
      public nuint Value { get; }
   }

   public interface ITemplateInt64 {
      public long Value { get; }
   }

   public struct TZero : ITemplateNUint, ITemplateNint, ITemplateInt64 {
      nuint ITemplateNUint.Value => 0;
      nint ITemplateNint.Value => 0;
      long ITemplateInt64.Value => 0;
   }

   public struct T10_000 : ITemplateNUint, ITemplateNint, ITemplateInt64 {
      nuint ITemplateNUint.Value => 10000;
      nint ITemplateNint.Value => 10000;
      long ITemplateInt64.Value => 10000;
   }

   public struct TInt64_10_000_000_000 : ITemplateInt64 {
      public long Value => 10_000_000_000;
   }

   public interface ITemplateString {
      public string Value { get; }
   }

   public interface ITemplateBindingFlags {
      public BindingFlags Value { get; }
   }

   public struct TBindingFlags_NonPublicInstance : ITemplateBindingFlags {
      public BindingFlags Value => BindingFlags.NonPublic | BindingFlags.Instance;
   }

   public interface IIntegerOperations<TInt> {
      [Pure] public TInt Increment(TInt v);
      [Pure] public TInt Decrement(TInt v);
      [Pure] public TInt Min(TInt a, TInt b);
      [Pure] public TInt Max(TInt a, TInt b);
      [Pure] public int Compare(TInt a, TInt b);

      [Pure] public TInt MinValue { get; }
      [Pure] public TInt MaxValue { get; }
   }

   public struct Int32Operations : IIntegerOperations<int> {
      public int Increment(int v) => v + 1;

      public int Decrement(int v) => v - 1;

      public int Min(int a, int b) => Math.Min(a, b);

      public int Max(int a, int b) => Math.Max(a, b);

      public int Compare(int a, int b) => a.CompareTo(b);

      public int MinValue => int.MinValue;
      
      public int MaxValue => int.MaxValue;
   }

   public interface ITemplateFunc<in T0, out TResult> {
      TResult Invoke(T0 arg0);
   }

   public interface ITemplatePredicate<T0> {
      bool Invoke(in T0 arg0);
   }

   public readonly struct IsGreaterThanOrEqualTo(int n) : ITemplatePredicate<int> {
      public bool Invoke(in int arg0) => arg0 >= n;
   }

   public interface ITemplateCast<TSource, TDestination> {
      TDestination Cast(TSource x);
   }

   public interface ITemplateBidirectionalCast<T1, T2> {
      T1 Cast(T2 x);
      T2 Cast(T1 x);
   }

   public interface ITemplateComparer<in T> : IComparer<T> {
      /// <returns>0 on equality, lt 0 if x lt y, gt 0 if x gt y</returns>
      new int Compare(T x, T y);
   }

   /// <summary>
   /// Sometimes, lt/gt can be implemented faster than CompareTo
   /// For example, CompareTo of integers might have a lt and gt branch,
   /// whereas lt/gt implementations alone would have no branch.
   /// </summary>
   /// <typeparam name="T"></typeparam>
   public interface IFastComparer<in T> {
      /// <returns>x gt y</returns>
      bool GreaterThan(T x, T y);

      /// <returns>x lt y</returns>
      bool LessThan(T x, T y);

      /// <returns>x lt y</returns>
      bool Equals(T x, T y);
   }

   public static class FastComparerExtensions {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static bool IsGreaterThan<T, TComparer>(this T x, T y, in TComparer comparer) where TComparer : struct, IFastComparer<T> {
         ref var cmp = ref Unsafe.AsRef(comparer);
         return cmp.GreaterThan(x, y);
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static bool IsLessThan<T, TComparer>(this T x, T y, in TComparer comparer) where TComparer : struct, IFastComparer<T> {
         ref var cmp = ref Unsafe.AsRef(comparer);
         return cmp.LessThan(x, y);
      }
   }

   public struct Int64Comparer : IComparer<long> {
      public int Compare(long x, long y) => x.CompareTo(y);
   }

   public struct Int32FastComparer : IFastComparer<int> {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public bool GreaterThan(int x, int y) => x > y;

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public bool LessThan(int x, int y) => x < y;

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public bool Equals(int x, int y) => x == y;
   }

   public struct Int64FastComparer : IFastComparer<long> {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public bool GreaterThan(long x, long y) => x > y;

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public bool LessThan(long x, long y) => x < y;

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public bool Equals(long x, long y) => x == y;
   }
}
