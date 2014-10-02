using System;

namespace ItzWarty.Test
{
   public class ReturnableWhenResult<T>
   {
      private NMockitoGlobals.InvocationAndMockState invocationAndMockState;
      private INMockitoSmartParameter[] smartParameters;

      public ReturnableWhenResult()
      {
         invocationAndMockState = NMockitoGlobals.GetLastInvocationAndMockState();
         invocationAndMockState.State.DecrementInvocationCounter(invocationAndMockState.Invocation);
         smartParameters = NMockitoSmartParameters.CopyAndClearSmartParameters();
      }

      public ReturnableWhenResult<T> ThenReturn(params T[] values)
      {
         foreach (var result in values) {
            invocationAndMockState.State.SetInvocationResult(invocationAndMockState.Invocation, smartParameters, new InvocationReturnResult(result));
         }
         return this;
      }

      public ReturnableWhenResult<T> ThenThrow(params Exception[] exceptions)
      {
         foreach (var exception in exceptions) {
            invocationAndMockState.State.SetInvocationResult(invocationAndMockState.Invocation, smartParameters, new InvocationThrowResult(exception));
         }
         return this;
      }
   }
}