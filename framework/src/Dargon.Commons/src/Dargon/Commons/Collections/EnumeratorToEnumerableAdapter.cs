using System.Collections.Generic;

namespace Dargon.Commons.Collections {
   public struct EnumeratorToEnumerableAdapter<TItem, TEnumerator> where TEnumerator : struct, IEnumerator<TItem> {
      private readonly TEnumerator enumerable;

      public EnumeratorToEnumerableAdapter(TEnumerator enumerable) {
         this.enumerable = enumerable;
      }

      public TEnumerator GetEnumerator() => enumerable;
   }

   public static class EnumeratorToEnumerableAdapter<TItem> {
      public static EnumeratorToEnumerableAdapter<TItem, TEnumerable> Create<TEnumerable>(TEnumerable enumerable) where TEnumerable : struct, IEnumerator<TItem> {
         return new EnumeratorToEnumerableAdapter<TItem, TEnumerable>(enumerable);
      }
   }
}