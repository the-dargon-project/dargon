using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Dargon.Commons {
   public static partial class ReflectionUtils {
      public static bool IsAutoProperty_Slow(this PropertyInfo prop) {
         var getter = prop.GetMethod;
         if (getter == null) {
            return false;
         }

         if (!getter.HasAttribute<CompilerGeneratedAttribute>()) {
            return false;
         }

         var declaringType = prop.DeclaringType.ThrowIfNull($"Null declaring type? for {prop}");
         var fields = declaringType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
         foreach (var f in fields) {
            if (f.Name.Contains(prop.Name) &&
                f.Name.Contains("BackingField") &&
                f.HasAttribute<CompilerGeneratedAttribute>()) {
               return true;
            }
         }

         return false;
      }
   }
}
