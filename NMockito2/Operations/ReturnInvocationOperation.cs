using NMockito2.Mocks;

namespace NMockito2.Operations {
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