using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dargon.Commons {
   public static class TLS<TScope> {
      public static T Get<T>() where T : new() {
         return TLS<TScope, T>.Value ??= new();
      }
   }

   public static class TLS<TScope, T> {
      [ThreadStatic] public static T Value;

      public static U Get<U>() where U : new() {
         return TLS<(TScope, T, TDummy)>.Get<U>();
      }

      private struct TDummy { }
   }
}
