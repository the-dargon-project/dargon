using System.Collections.Generic;
using NMockito.Operations;
using NMockito.SmartParameters;
using NMockito.Utilities;

namespace NMockito.Mocks {
   public class InvocationOperationManager {
      private readonly SmartParameterCollection smartParameterCollection;
      private readonly List<InvocationOperation> invocationOperations = new List<InvocationOperation>();
      private int jumpBackIndex = 0;
      private int nextIndex = 0;

      public InvocationOperationManager(SmartParameterCollection smartParameterCollection) {
         this.smartParameterCollection = smartParameterCollection;
      }

      public bool Matches(InvocationDescriptor invocationDescriptor) {
         return smartParameterCollection.Matches(invocationDescriptor);
      }

      public void Execute(InvocationDescriptor invocationDescriptor) {
         invocationDescriptor.Invocation.ReturnValue = invocationDescriptor.Method.GetDefaultReturnValue();

         var index = nextIndex;
         if (index == invocationOperations.Count) {
            index = jumpBackIndex;
         }

         while (index < invocationOperations.Count) {
            var invocationOperation = invocationOperations[index];
            index++;

            var execution = invocationOperation.Execute(invocationDescriptor);
            if (execution == Execution.Stop) {
               break;
            }
         }

         nextIndex = index;
         if (index != invocationOperations.Count) {
            jumpBackIndex = index;
         }
      }

      public void AddInvocationOperation(InvocationOperation invocationOperation) {
         invocationOperations.Add(invocationOperation);
      }
   }
}