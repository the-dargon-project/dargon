using NMockito.Mocks;

namespace NMockito.Operations {
   public class ReturnInvocationOperation : InvocationOperation {
      private readonly object returnValue;

      public ReturnInvocationOperation(object returnValue) {
         this.returnValue = returnValue;
      }

      public Execution Execute(InvocationDescriptor invocationDescriptor) {
         invocationDescriptor.Invocation.ReturnValue = returnValue;
         return Execution.Stop;
      }
   }
}