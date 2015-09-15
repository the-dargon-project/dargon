using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NMockito2.Mocks;
using NMockito2.Utilities;

namespace NMockito2.Transformations {
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
               if (outValue == null && realType.IsValueType) {
                  outValue = realType.GetDefaultValue();
               }
               invocationDescriptor.Invocation.Arguments[i] = outValue;
            }
         }
      }
   }
}
