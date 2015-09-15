using NMockito2.Mocks;

namespace NMockito2.Transformations {
   public interface InvocationTransformation {
      bool IsApplicable(InvocationDescriptor invocationDescriptor);
      void Forward(InvocationDescriptor invocationDescriptor);
      void Backward(InvocationDescriptor invocationDescriptor);
   }
}