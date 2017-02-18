using System.Collections.Concurrent;
using NMockito.Operations;

namespace NMockito.Mocks {
   public class InvocationOperationManagerFinder {
      private readonly ConcurrentDictionary<MockAndMethod, MockMethodInvocationOperationManagerFinder> table = new ConcurrentDictionary<MockAndMethod, MockMethodInvocationOperationManagerFinder>();

      public bool TryFind(InvocationDescriptor invocationDescriptor, out InvocationOperationManager invocationOperationManager) {
         invocationOperationManager = null;
         var mockAndMethod = new MockAndMethod(invocationDescriptor);
         MockMethodInvocationOperationManagerFinder mockMethodInvocationOperationManagerFinder;
         return table.TryGetValue(mockAndMethod, out mockMethodInvocationOperationManagerFinder) &&
                mockMethodInvocationOperationManagerFinder.TryFind(invocationDescriptor, out invocationOperationManager);
      }

      public void AddInvocationOperation(InvocationDescriptor invocationDescriptor, InvocationOperation invocationOperation) {
         var mockAndMethod = new MockAndMethod(invocationDescriptor);
         var sub = table.GetOrAdd(mockAndMethod, add => new MockMethodInvocationOperationManagerFinder());
         sub.AddInvocationOperation(invocationDescriptor, invocationOperation);
      }
   }
}