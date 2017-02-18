using NMockito.Mocks;

namespace NMockito.Transformations {
   public interface InvocationTransformation {
      bool IsApplicable(InvocationDescriptor invocationDescriptor);
      void Forward(InvocationDescriptor invocationDescriptor);
      void Backward(InvocationDescriptor invocationDescriptor);
   }
}