using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dargon.Commons {
   public static class LazyStatics {
      public static Lazy<T> Lazy<T>(Func<T> func) => new(func);
   }
}
