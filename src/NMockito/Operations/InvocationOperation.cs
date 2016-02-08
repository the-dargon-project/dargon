using NMockito.Mocks;

namespace NMockito.Operations {
   public interface InvocationOperation {
      Execution Execute(InvocationDescriptor invocationDescriptor);
   }
}