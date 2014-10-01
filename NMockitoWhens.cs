using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItzWarty.Test
{
   internal static class NMockitoWhens
   {
      private static NMockitoGlobals.InvocationAndMockState invocationAndMockState;

      public static void HandleWhenInvocation() 
      { 
         invocationAndMockState = NMockitoGlobals.GetLastInvocationAndMockState(); 
      }

      public static void HandleInvocationResult(IInvocationResult result)
      {
         var invocation = invocationAndMockState.Invocation;
         var state = invocationAndMockState.State;
         state.SetInvocationResult(invocation, result);
      }
   }
}
