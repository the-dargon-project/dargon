using System.Collections.Generic;
using System.Linq;
using NMockito.Operations;

namespace NMockito.Mocks {
   public class MockMethodInvocationOperationManagerFinder {
      private readonly object synchronization = new object();
      private readonly IList<InvocationOperationManager> operationManagers = new List<InvocationOperationManager>();

      public bool TryFind(InvocationDescriptor invocationDescriptor, out InvocationOperationManager invocationOperationManager) {
         lock (synchronization) {
            invocationOperationManager = operationManagers.FirstOrDefault(
               operationManager => operationManager.Matches(invocationDescriptor));
            return invocationOperationManager != null;
         }
      }

      public void AddInvocationOperation(InvocationDescriptor invocationDescriptor, InvocationOperation invocationOperation) {
         lock (synchronization) {
            InvocationOperationManager invocationOperationManager;
            if (!TryFind(invocationDescriptor, out invocationOperationManager)) {
               invocationOperationManager = new InvocationOperationManager(invocationDescriptor.SmartParameters);
               operationManagers.Add(invocationOperationManager);
            }
            invocationOperationManager.AddInvocationOperation(invocationOperation);
         }
      }
   }
}