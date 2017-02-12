using System.Collections.Generic;

namespace Dargon.Commons.Collections {
   public class ImmutableSet {
      public static IReadOnlySet<T> Of<T>() {
         return new HashSet<T>().AsReadOnlySet();
      }

      public static IReadOnlySet<T> Of<T>(params T[] values) {
         return new HashSet<T>(values).AsReadOnlySet();
      }
   }
}