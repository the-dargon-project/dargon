using NMockito.Mocks;

namespace NMockito.Operations {
   public class SetOutsInvocationOperation : InvocationOperation {
      private readonly object[] outValues;

      public SetOutsInvocationOperation(params object[] outValues) {
         this.outValues = outValues;
      }

      public Execution Execute(InvocationDescriptor invocationDescriptor) {
         var parameters = invocationDescriptor.Method.GetParameters();

         var nextOutIndex = 0;
         for (var parameterIndex = 0;
              parameterIndex < parameters.Length && nextOutIndex < outValues.Length;
              parameterIndex++) {
            var parameter = parameters[parameterIndex];
            if (parameter.IsOut) {
               invocationDescriptor.Arguments[parameterIndex] = outValues[nextOutIndex];
               nextOutIndex++;
            }
         }

         return Execution.Continue;
      }
   }
}