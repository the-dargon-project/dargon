using System;
using System.Linq;

namespace Dargon.Vox.Utilities {
   public class GenericTypeUnpacker<T> {
      private static readonly Type[] arguments;

      static GenericTypeUnpacker() {
         if (!typeof(T).IsGenericType) return;

         Definition = typeof(T).GetGenericTypeDefinition();
         arguments = typeof(T).GetGenericArguments();

         T1 = arguments.Length > 0 ? arguments[0] : null;
         T2 = arguments.Length > 1 ? arguments[1] : null;
         T3 = arguments.Length > 2 ? arguments[2] : null;
      }

      public static Type Definition { get; set; }
      public static Type T1 { get; private set; }
      public static Type T2 { get; private set; }
      public static Type T3 { get; private set; }

      public static Type Build(Type t) {
         var genericArgs = t.GetGenericArguments();
         return t.MakeGenericType(arguments.Take(genericArgs.Length).ToArray());
      }
   }
}
