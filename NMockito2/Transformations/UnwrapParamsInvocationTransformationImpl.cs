using System;
using System.Diagnostics;
using System.Linq;
using NMockito2.Mocks;
using NMockito2.Utilities;

namespace NMockito2.Transformations {
   public class UnwrapParamsInvocationTransformationImpl : InvocationTransformation {
      public bool IsApplicable(InvocationDescriptor invocationDescriptor) {
         Type paramsType;
         if (!invocationDescriptor.Method.TryGetParamsType(out paramsType)) return false;
         if (invocationDescriptor.Arguments.Last() == null) return false;
         return true;
      }

      public void Forward(InvocationDescriptor invocationDescriptor) {
         var arguments = invocationDescriptor.Arguments;
         var argumentCount = arguments.Length;
         var preParamsArgumentCount = argumentCount - 1;
         var paramsArray = (Array)arguments.Last();

         var newArguments = new object[preParamsArgumentCount + paramsArray.Length];
         Array.Copy(arguments, 0, newArguments, 0, preParamsArgumentCount);
         Array.Copy(paramsArray, 0, newArguments, preParamsArgumentCount, paramsArray.Length);
         invocationDescriptor.Arguments = newArguments;
      }

      public void Backward(InvocationDescriptor invocationDescriptor) {
         var parameterCount = invocationDescriptor.Method.GetParameters().Length;
         var preParamsArgumentsCount = parameterCount - 1;
         var arguments = invocationDescriptor.Arguments;

         Type paramsType = invocationDescriptor.Method.GetParamsType();
         var newParams = Array.CreateInstance(paramsType.GetElementType(), arguments.Length - parameterCount + 1);
         Array.Copy(arguments, preParamsArgumentsCount, newParams, 0, newParams.Length);

         var newArguments = new object[parameterCount];
         Array.Copy(arguments, 0, newArguments, 0, preParamsArgumentsCount);
         newArguments[parameterCount - 1] = newParams;

         invocationDescriptor.Arguments = newArguments;
      }
   }
}