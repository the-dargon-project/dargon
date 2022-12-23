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
   public class TDummy : TArg<TDummy> { }
   public class TTrue : TArg<TTrue> {}
   public class TFalse : TArg<TFalse> { }

   public record struct TypeId(int Value);

   public static class TypeIds<TNamespace> {
      // ReSharper disable once StaticMemberInGenericType
      private static int next;

      public static TypeId Get<T>() => Inner<T>.Value;

      private static class Inner<T> {
         // ReSharper disable once StaticMemberInGenericType
         public static TypeId Value { get; } = new(Interlocked2.PostIncrement(ref next));
      }
   }


   public class TypeCounter<TType> {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static int GetIndex<TKey>() => TypeIds<DummyNamespace>.Get<TKey>().Value;

      private struct DummyNamespace {}
   }

   public interface ITemplateInt64 {
      public long Value { get; }
   }

   public struct TInt64_10_000 : ITemplateInt64 {
      public long Value => 10000;
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
}
