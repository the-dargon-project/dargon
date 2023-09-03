using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Dargon.Commons {
   public static class Unsafe2 {
      /// <summary>
      /// Usage (equivalent):
      ///   x = ref IncrementRef(ref x)
      /// Note, you cannot do:
      ///   IncrementRef(ref x) // doesn't change x's address
      /// </summary>
      /// <param name="x">reference whose address is to be incremented</param>
      /// <returns>x</returns>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static ref T IncrementRef<T>(ref T x) => ref Unsafe.Add<T>(ref x, 1);
   }
}
