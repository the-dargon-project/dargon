using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using NMockito2.Counters;
using NMockito2.Mocks;

namespace NMockito2.Verification {
   public class VerificationOperationsProxy {
      private readonly InvocationStage invocationStage;
      private readonly VerificationOperations verificationOperations;
      private readonly VerificationMockFactory verificationMockFactory;

      public VerificationOperationsProxy(InvocationStage invocationStage, VerificationOperations verificationOperations, VerificationMockFactory verificationMockFactory) {
         this.invocationStage = invocationStage;
         this.verificationOperations = verificationOperations;
         this.verificationMockFactory = verificationMockFactory;
      }

      public void Verify(Action action) {
         action();
         Verify();
      }

      public void Verify() {
         var invocationDescriptor = invocationStage.ReleaseLastInvocation();
         verificationOperations.VerifyInvocation(invocationDescriptor, new AnyCounter());
      }

      public TMock Verify<TMock>(TMock mock) where TMock : class {
         return verificationMockFactory.Create(mock, this);
      }
   }
   
   public class VerificationMockFactory {
      private readonly ProxyGenerator proxyGenerator;

      public VerificationMockFactory(ProxyGenerator proxyGenerator) {
         this.proxyGenerator = proxyGenerator;
      }

      public TMock Create<TMock>(TMock mock, VerificationOperationsProxy verificationOperationsProxy) where TMock : class {
         if (typeof(TMock).IsInterface) {
            return proxyGenerator.CreateInterfaceProxyWithoutTarget<TMock>(
               new VerificationMockInterceptor<TMock>(
                  mock,
                  verificationOperationsProxy));
         } else {
            return proxyGenerator.CreateClassProxy<TMock>(
               new VerificationMockInterceptor<TMock>(
                  mock,
                  verificationOperationsProxy));
         }
      }
   }

   public class VerificationMockInterceptor<TMock> : IInterceptor {
      private readonly TMock mock;
      private readonly VerificationOperationsProxy verificationOperationsProxy;

      public VerificationMockInterceptor(TMock mock, VerificationOperationsProxy verificationOperationsProxy) {
         this.mock = mock;
         this.verificationOperationsProxy = verificationOperationsProxy;
      }

      public void Intercept(IInvocation invocation) {
         invocation.ReturnValue = invocation.Method.Invoke(mock, invocation.Arguments);
         verificationOperationsProxy.Verify();
      }
   }
}
