using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NMockito2.Utilities {
   public static class ReflectionUtilities {
      public static object GetDefaultReturnValue(this MethodInfo methodInfo) {
         return methodInfo.ReturnType.GetDefaultValue();
      }

      public static object GetDefaultValue(this Type type) {
         return type.IsValueType && type != typeof(void) ? Activator.CreateInstance(type) : null;
      }

      public static Type GetParamsType(this MethodInfo methodInfo) {
         Type type;
         if (!TryGetParamsType(methodInfo, out type)) {
            throw new ArgumentException("The method does not have a params argument.");
         }
         return type;
      }

      public static bool TryGetParamsType(this MethodInfo methodInfo, out Type paramsArrayType) {
         paramsArrayType = null;
         var parameters = methodInfo.GetParameters();
         if (parameters.Any()) {
            var lastParameter = parameters.Last();
            var paramsAttribute = lastParameter.GetCustomAttribute<ParamArrayAttribute>();
            if (paramsAttribute != null) {
               paramsArrayType = lastParameter.ParameterType;
            }
         }
         return paramsArrayType != null;
      }
   }
}
