using System;

namespace ItzWarty.Test
{
   public class ReturnableWhenResult<T>
   {
      public ReturnableWhenResult<T> ThenReturn(params T[] values)
      {
         foreach (var result in values) {
            NMockitoWhens.HandleInvocationResult(new InvocationReturnResult(result));
         }
         return this;
      }

      public ReturnableWhenResult<T> ThenThrow(params Exception[] exceptions)
      {
         foreach (var exception in exceptions) {
            NMockitoWhens.HandleInvocationResult(new InvocationThrowResult(exception));
         }
         return this;
      }
   }
}