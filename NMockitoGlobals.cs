using Castle.DynamicProxy;

namespace NMockito
{
   internal static class NMockitoGlobals
   {
      private static InvocationAndMockState lastInvocationAndMockState;

      public static InvocationAndMockState GetLastInvocationAndMockState()
      {
         var result = lastInvocationAndMockState;
         lastInvocationAndMockState = null;
         return result;
      }

      public static void SetLastInvocationAndMockState(IInvocation invocation, MockState state) { lastInvocationAndMockState = new InvocationAndMockState(invocation, state); }

      public class InvocationAndMockState
      {
         private readonly IInvocation invocation;
         private readonly MockState state;

         public InvocationAndMockState(IInvocation invocation, MockState state)
         {
            this.invocation = invocation;
            this.state = state;
         }

         public IInvocation Invocation { get { return invocation; } }

         public MockState State { get { return state; } }
      }
   }
}