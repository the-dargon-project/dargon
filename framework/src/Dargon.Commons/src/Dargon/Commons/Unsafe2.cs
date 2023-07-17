using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Dargon.Commons {
   public static class Unsafe2 {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static ref T IncrementRef<T>(ref T x) => ref Unsafe.Add<T>(ref x, 1);
   }
}
