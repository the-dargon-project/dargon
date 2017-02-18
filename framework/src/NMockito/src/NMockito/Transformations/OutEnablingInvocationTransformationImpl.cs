using System.Linq;
using System.Reflection;
using NMockito.Mocks;
using NMockito.Utilities;

namespace NMockito.Transformations {
   public class OutEnablingInvocationTransformationImpl : InvocationTransformation {
      public bool IsApplicable(InvocationDescriptor invocationDescriptor) {
            var parameters = invocationDescriptor.Method.GetParameters();
         return parameters.Any(parameter => parameter.IsOut);
      }

      public void Forward(InvocationDescriptor invocationDescriptor) {
         var parameters = invocationDescriptor.Method.GetParameters();
         for (var i = 0; i < parameters.Length; i++) {
            var parameter = parameters[i];
            if (parameter.IsOut) {
               invocationDescriptor.Arguments[i] = parameter.ParameterType.GetDefaultValue();
            }
         }
      }

      public void Backward(InvocationDescriptor invocationDescriptor) {
         var parameters = invocationDescriptor.Method.GetParameters();
         for (var i = 0; i < parameters.Length; i++) {
            var parameter = parameters[i];
            if (parameter.IsOut) {
               var outValue = invocationDescriptor.Arguments[i];
               var realType = parameter.ParameterType.GetElementType();
               if (outValue == null && realType.GetTypeInfo().IsValueType) {
                  outValue = realType.GetDefaultValue();
               }
               invocationDescriptor.Invocation.Arguments[i] = outValue;
            }
         }
      }
   }
}
