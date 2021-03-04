using System.Collections;
using System.Collections.Generic;

namespace Dargon.Commons.Collections {
   public class ReadOnlySetWrapper<T> : IReadOnlySet<T> {
      private readonly ISet<T> inner;

      public ReadOnlySetWrapper(ISet<T> inner) {
         this.inner = inner;
      }

      public int Count => inner.Count;

      public bool Overlaps(IEnumerable<T> other) => inner.Overlaps(other);

      public bool SetEquals(IEnumerable<T> other) => inner.SetEquals(other);

      public void CopyTo(T[] array, int arrayIndex) => inner.CopyTo(array, arrayIndex);

      public bool Contains(T item) => inner.Contains(item);
      public bool IsProperSubsetOf(IEnumerable<T> other) => inner.IsProperSubsetOf(other);

      public bool IsProperSupersetOf(IEnumerable<T> other) => inner.IsProperSupersetOf(other);

      public bool IsSubsetOf(IEnumerable<T> other) => inner.IsSubsetOf(other);

      public bool IsSupersetOf(IEnumerable<T> other) => inner.IsSupersetOf(other);

      public IEnumerator<T> GetEnumerator() => inner.GetEnumerator();
      IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
   }
}
