using System;
using System.Reflection;
using Castle.DynamicProxy;
using NMockito2.Assertions;
using NMockito2.Utilities;

namespace NMockito2.Fluent {
   public class AssertExceptionCatchingInterceptor<TMock> : IInterceptor {
      private readonly TMock mock;

      public AssertExceptionCatchingInterceptor(TMock mock) {
         this.mock = mock;
      }

      public void Intercept(IInvocation invocation) {
         try {
            invocation.ReturnValue = invocation.Method.Invoke(mock, invocation.Arguments);
            NMockitoInstance.Instance.SetLastException(null);
         } catch (TargetInvocationException targetInvocationException) {
            var e = targetInvocationException.InnerException;
            invocation.ReturnValue = invocation.Method.GetDefaultReturnValue();
            NMockitoInstance.Instance.SetLastException(e);
         }
      }
   }
}