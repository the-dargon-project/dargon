using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Dargon.Commons.Collections {
   public struct EnumeratorToEnumerableAdapter<TItem, TEnumerator> : IEnumerable<TItem> where TEnumerator : struct, IEnumerator<TItem> {
      private readonly TEnumerator enumerator;

      public EnumeratorToEnumerableAdapter(TEnumerator enumerator) {
         this.enumerator = enumerator;
      }

      public TEnumerator GetEnumerator() => enumerator;
      IEnumerator<TItem> IEnumerable<TItem>.GetEnumerator() => GetEnumerator();
      IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
   }

   public static class StructLinq<T> {
      public static StructLinqWhere<T, TInnerEnumerator> Where<TInnerEnumerator>(TInnerEnumerator inner, Func<T, bool> cond) where TInnerEnumerator : struct, IEnumerator<T> {
         return new StructLinqWhere<T, TInnerEnumerator>(inner, cond);
      }

      public static StructLinqWhere<T, TInnerEnumerator> Where<TInnerEnumerator, TDelegateStaticAssertMemo>(TInnerEnumerator inner, Func<T, bool> cond, TDelegateStaticAssertMemo dummy) 
         where TInnerEnumerator : struct, IEnumerator<T> 
         where TDelegateStaticAssertMemo : struct {
         DelegateMethodIsStatic<TDelegateStaticAssertMemo>.VerifyOnce(cond);
         return new StructLinqWhere<T, TInnerEnumerator>(inner, cond);
      }

      private static class DelegateMethodIsStatic<TFuncStaticAssertMemo> where TFuncStaticAssertMemo : struct {
         private static bool verified = false;

         // when invoked, the static assert is executed
         public static void VerifyOnce(Delegate del) {
            if (verified) return;

            var methodInfo = del.GetMethodInfo();
            methodInfo.IsStatic.AssertIsTrue();

            verified = true;
         }
      }
   }

   public struct StructLinqWhere<T, TInnerEnumerator> : IEnumerator<T>, IEnumerable<T> where TInnerEnumerator : IEnumerator<T> {
      private TInnerEnumerator inner;
      private Func<T, bool> cond;

      public StructLinqWhere(TInnerEnumerator inner, Func<T, bool> cond) {
         this.inner = inner;
         this.cond = cond;
      }

      public bool MoveNext() {
         // iterate inner iterator until current passes cond.
         while (inner.MoveNext()) {
            if (cond(inner.Current)) {
               return true;
            }
         }

         // if we pass the end of inner, we can't move to next.
         return false;
      }

      public void Reset() => inner.Reset();
      public T Current => inner.Current;
      object IEnumerator.Current => Current;
      public void Dispose() => inner.Dispose();

      public IEnumerator<T> GetEnumerator() => this;
      IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
   }

   public static class EnumeratorToEnumerableAdapter<TItem> {
      public static EnumeratorToEnumerableAdapter<TItem, TEnumerator> Create<TEnumerator>(TEnumerator enumerator) where TEnumerator : struct, IEnumerator<TItem> {
         return new EnumeratorToEnumerableAdapter<TItem, TEnumerator>(enumerator);
      }
   }
}