using System.Collections.Generic;

namespace Dargon.Commons.Utilities;

public class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class {
   public bool Equals(T x, T y) {
      return ReferenceEquals(x, y);
   }

   public int GetHashCode(T obj) {
      return obj.GetObjectIdHash();
   }
}