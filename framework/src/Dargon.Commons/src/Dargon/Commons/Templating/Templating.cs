using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
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
}
