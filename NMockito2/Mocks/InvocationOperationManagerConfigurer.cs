using NMockito2.Operations;

namespace NMockito2.Mocks {
   public class InvocationOperationManagerConfigurer {
      private readonly InvocationOperationManagerFinder invocationOperationManagerFinder;
      private readonly InvocationStage invocationStage;

      public InvocationOperationManagerConfigurer(InvocationOperationManagerFinder invocationOperationManagerFinder, InvocationStage invocationStage) {
         this.invocationOperationManagerFinder = invocationOperationManagerFinder;
         this.invocationStage = invocationStage;
      }

      public void AddInvocationOperation(InvocationOperation invocationOperation) {
         var invocationDescriptor = invocationStage.GetLastInvocation();
         invocationOperationManagerFinder.AddInvocationOperation(invocationDescriptor, invocationOperation);
      }
   }
}