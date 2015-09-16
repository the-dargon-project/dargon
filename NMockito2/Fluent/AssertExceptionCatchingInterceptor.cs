using System;
using System.Reflection;
using Castle.DynamicProxy;
using NMockito2.Assertions;
using NMockito2.Utilities;

namespace NMockito2.Fluent {
   public class AssertExceptionCatchingInterceptor<TMock> : IInterceptor {
      private readonly TMock mock;
      private readonly FluentExceptionAssertor fluentExceptionAssertor;

      public AssertExceptionCatchingInterceptor(TMock mock, FluentExceptionAssertor fluentExceptionAssertor) {
         this.mock = mock;
         this.fluentExceptionAssertor = fluentExceptionAssertor;
      }

      public void Intercept(IInvocation invocation) {
         try {
            invocation.ReturnValue = invocation.Method.Invoke(mock, invocation.Arguments);
            fluentExceptionAssertor.SetLastException(null);
         } catch (TargetInvocationException targetInvocationException) {
            var e = targetInvocationException.InnerException;
            invocation.ReturnValue = invocation.Method.GetDefaultReturnValue();
            fluentExceptionAssertor.SetLastException(e);
         }
      }
   }
}