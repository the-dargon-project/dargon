using System;
using Castle.DynamicProxy;

namespace NMockito
{
   internal interface IInvocationExecutor
   {
      object Execute(IInvocation invocation);
      bool IsTerminal { get; }
   }

   internal class InvocationThrowExecutor : IInvocationExecutor
   {
      private readonly Exception exception;
      public InvocationThrowExecutor(Exception exception) { this.exception = exception; }
      public object Execute(IInvocation invocation) { throw exception; }
      public bool IsTerminal { get { return true; } }
   }

   internal class InvocationReturnExecutor : IInvocationExecutor {
      private readonly object value;
      public InvocationReturnExecutor(object value) { this.value = value; }
      public object Execute(IInvocation invocation) { return value; }
      public bool IsTerminal { get { return true; } }
   }

   internal class InvocationSetExecutor : IInvocationExecutor {
      private readonly object mock;
      private readonly object value;
      public InvocationSetExecutor(object mock, object value) {
         this.mock = mock;
         this.value = value;
      }

      public object Execute(IInvocation invocation) {
         var methodParameters = invocation.Method.GetParameters();
         for (var i = 0; i < invocation.Arguments.Length; i++) {
            if ((methodParameters[i].IsOut || methodParameters[i].ParameterType.IsByRef) && invocation.Arguments[i].Equals(mock)) {
               invocation.Arguments[i] = value;
            }
         }
         return null;
      }
      public bool IsTerminal { get { return false; } }
   }

   internal class InvocationExecExecutor : IInvocationExecutor {
      private readonly Action action;

      public InvocationExecExecutor(Action action) {
         this.action = action;
      }

      public object Execute(IInvocation invocation) {
         action();
         return null;
      }
      public bool IsTerminal { get { return false; } }
   }
}