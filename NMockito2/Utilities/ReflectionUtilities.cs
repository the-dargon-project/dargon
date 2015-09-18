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

      public static bool IsEqualTo(this Type self, Type other) {
         if (self == other) {
            return true;
         } else if (self.IsGenericParameter && other.IsGenericParameter) {
            return self.GenericParameterAttributes == other.GenericParameterAttributes &&
                   self.GetGenericParameterConstraints().SequenceEqual(other.GetGenericParameterConstraints());
         } else if (self.ContainsGenericParameters && other.ContainsGenericParameters) {
            return self.GetGenericTypeDefinition() == other.GetGenericTypeDefinition() &&
                   self.GetGenericArguments().Zip(other.GetGenericArguments(), IsEqualTo).All(x => x);
         } else {
            return false;
         }
      }
   }
}
