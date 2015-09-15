using NMockito2.Mocks;

namespace NMockito2.Operations {
   public interface InvocationOperation {
      Execution Execute(InvocationDescriptor invocationDescriptor);
   }
}