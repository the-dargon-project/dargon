using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dargon.Hydrous.Impl {
   public static class SomeHelperClass {
      public static unsafe Guid AddToGuidSomehow(Guid guid, int value) {
         var bytes = guid.ToByteArray();

         // sue me.
         fixed (byte* pBytes = bytes) {
            *(int*)pBytes += value;
         }

         return new Guid(bytes);
      }

      public static string ToShortString(this Guid x) {
         return x.ToString("n").Substring(0, 6);
      }
   }
}
