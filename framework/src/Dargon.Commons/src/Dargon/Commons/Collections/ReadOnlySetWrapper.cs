using System.Collections;
using System.Collections.Generic;

namespace Dargon.Commons.Collections {
   public class ReadOnlySetWrapper<T> : IReadOnlySet<T> {
      private readonly ISet<T> inner;

      public ReadOnlySetWrapper(ISet<T> inner) {
         this.inner = inner;
      }

      public int Count => inner.Count;

      public bool SetEquals(IEnumerable<T> other) => inner.SetEquals(other);

      public void CopyTo(T[] array, int arrayIndex) => inner.CopyTo(array, arrayIndex);

      public bool Contains(T item) => inner.Contains(item);

      public IEnumerator<T> GetEnumerator() => inner.GetEnumerator();
      IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
   }
}
