using System.Collections;
using System.Collections.Generic;

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

   public struct ArrayEnumerator2<T> : IEnumerator<T> {
      private readonly T[] arr;
      private int i;

      public ArrayEnumerator2(T[] arr) {
         this.arr = arr;
         this.i = -1;
      }

      public bool MoveNext() {
         if (i + 1 == arr.Length) {
            return false;
         }

         i++;
         return true;
      }

      public void Reset() => i = -1;

      public T Current => arr[i];
      object IEnumerator.Current => Current;

      public void Dispose() { }
   }

   public static class EnumeratorToEnumerableAdapter<TItem> {
      public static EnumeratorToEnumerableAdapter<TItem, TEnumerator> Create<TEnumerator>(TEnumerator enumerator) where TEnumerator : struct, IEnumerator<TItem> {
         return new EnumeratorToEnumerableAdapter<TItem, TEnumerator>(enumerator);
      }
   }
}