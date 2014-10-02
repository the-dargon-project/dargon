using System;

namespace NMockito
{
   public class WhenContext<T>
   {
      private NMockitoGlobals.InvocationAndMockState invocationAndMockState;
      private INMockitoSmartParameter[] smartParameters;

      public WhenContext()
      {
         invocationAndMockState = NMockitoGlobals.GetLastInvocationAndMockState();
         invocationAndMockState.State.DecrementInvocationCounter(invocationAndMockState.Invocation);
         smartParameters = NMockitoSmartParameters.CopyAndClearSmartParameters();
      }

      public WhenContext<T> ThenReturn(params T[] values)
      {
         foreach (var result in values) {
            invocationAndMockState.State.SetInvocationResult(invocationAndMockState.Invocation, smartParameters, new InvocationReturnResult(result));
         }
         return this;
      }

      public WhenContext<T> ThenThrow(params Exception[] exceptions)
      {
         foreach (var exception in exceptions) {
            invocationAndMockState.State.SetInvocationResult(invocationAndMockState.Invocation, smartParameters, new InvocationThrowResult(exception));
         }
         return this;
      }
   }
}