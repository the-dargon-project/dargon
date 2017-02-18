using NMockito.Mocks;
using NMockito.SmartParameters;

namespace NMockito.Transformations {
   public class CreateImplicitEqualitySmartParametersInvocationTransformationImpl : InvocationTransformation {
      public bool IsApplicable(InvocationDescriptor invocationDescriptor) => true;

      public void Forward(InvocationDescriptor invocationDescriptor) {
         var arguments = invocationDescriptor.Arguments;
         var smartParameters = new SmartParameter[arguments.Length];

         for (var i = 0; i < arguments.Length; i++) {
            smartParameters[i] = new EqualitySmartParameter(arguments[i]);
         }

         invocationDescriptor.SmartParameters = new SmartParameterCollection(smartParameters);
      }

      public void Backward(InvocationDescriptor invocationDescriptor) {
         invocationDescriptor.SmartParameters = null;
      }
   }
}