using System.Collections.Generic;

namespace Dargon.Commons.Collections {
   public interface IReadOnlySet<T> : IReadOnlyCollection<T> {
      bool SetEquals(IEnumerable<T> other);
      void CopyTo(T[] array, int arrayIndex);
      bool Contains(T element);
   }
}