using System.Collections;
using System.Collections.Generic;

namespace Dargon.Commons.Collections {
   public struct EnumeratorToEnumerableAdapter<TItem, TEnumerator> : IEnumerable<TItem> where TEnumerator : struct, IEnumerator<TItem> {
      private readonly TEnumerator enumerable;

      public EnumeratorToEnumerableAdapter(TEnumerator enumerable) {
         this.enumerable = enumerable;
      }

      public TEnumerator GetEnumerator() => enumerable;
      IEnumerator<TItem> IEnumerable<TItem>.GetEnumerator() => GetEnumerator();
      IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
   }

   public static class EnumeratorToEnumerableAdapter<TItem> {
      public static EnumeratorToEnumerableAdapter<TItem, TEnumerable> Create<TEnumerable>(TEnumerable enumerable) where TEnumerable : struct, IEnumerator<TItem> {
         return new EnumeratorToEnumerableAdapter<TItem, TEnumerable>(enumerable);
      }
   }
}