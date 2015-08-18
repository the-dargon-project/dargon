using System;
using Castle.DynamicProxy.Generators.Emitters;

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
         if (values == null || values.Length == 0)
            values = new T[] { default(T) };

         foreach (var result in values) {
            invocationAndMockState.State.AddInvocationExecutor(invocationAndMockState.Invocation, smartParameters, new InvocationReturnExecutor(result));
         }
         return this;
      }

      public WhenContext<T> ThenThrow(params Exception[] exceptions)
      {
         foreach (var exception in exceptions) {
            invocationAndMockState.State.AddInvocationExecutor(invocationAndMockState.Invocation, smartParameters, new InvocationThrowExecutor(exception));
         }
         return this;
      }

      public WhenContext<T> Set<TMock>(TMock mock, TMock value) {
         if (mock.Equals(default(TMock))) {
            throw new ArgumentNullException($"Mock placeholders are not permitted to be the default value. The placeholder for type {typeof(TMock).FullName} has value {value}.");
         }
         invocationAndMockState.State.AddInvocationExecutor(invocationAndMockState.Invocation, smartParameters, new InvocationSetExecutor(mock, value));
         return this;
      }

      public WhenContext<T> Exec(params Action[] actions) {
         foreach (var action in actions) {
            invocationAndMockState.State.AddInvocationExecutor(invocationAndMockState.Invocation, smartParameters, new InvocationExecExecutor(action));
         }
         return this;
      }
   }
}