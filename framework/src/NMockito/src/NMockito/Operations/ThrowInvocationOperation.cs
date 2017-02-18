using System;
using NMockito.Mocks;

namespace NMockito.Operations {
   public class ThrowInvocationOperation : InvocationOperation {
      private readonly Exception exception;

      public ThrowInvocationOperation(Exception exception) {
         this.exception = exception;
      }

      public Execution Execute(InvocationDescriptor invocationDescriptor) {
         invocationDescriptor.Exception = exception;;
         return Execution.Stop;
      }
   }
}