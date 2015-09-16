using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NMockito2.Mocks;
using NMockito2.Verification;

namespace NMockito2.Expectations {
   public class ExpectationFactory {
      private readonly InvocationStage invocationStage;
      private readonly InvocationOperationManagerFinder invocationOperationManagerFinder;
      private readonly VerificationInvocationsContainer verificationInvocationsContainer;

      public ExpectationFactory(InvocationStage invocationStage, InvocationOperationManagerFinder invocationOperationManagerFinder, VerificationInvocationsContainer verificationInvocationsContainer) {
         this.invocationStage = invocationStage;
         this.invocationOperationManagerFinder = invocationOperationManagerFinder;
         this.verificationInvocationsContainer = verificationInvocationsContainer;
      }

      public Expectation Create(Action action, bool expectInvocation) {
         action();
         return Create(expectInvocation);
      }

      public Expectation Create(bool expectInvocation) {
         var invocationDescriptor = invocationStage.ReleaseLastInvocation();
         if (expectInvocation) {
            verificationInvocationsContainer.ExpectUnverifiedInvocation(invocationDescriptor);
         }
         return new Expectation(invocationDescriptor, invocationOperationManagerFinder);
      }

      public Expectation<TResult> Create<TResult>(bool expectInvocation) {
         var invocationDescriptor = invocationStage.ReleaseLastInvocation();
         if (expectInvocation) {
            verificationInvocationsContainer.ExpectUnverifiedInvocation(invocationDescriptor);
         }
         return new Expectation<TResult>(invocationDescriptor, invocationOperationManagerFinder);
      }

      public Expectation<TOut1, TResult> Create<TOut1, TResult>(Func<TOut1, TResult> func, bool expectInvocation) {
         func(default(TOut1));
         var invocationDescriptor = invocationStage.ReleaseLastInvocation();
         if (expectInvocation) {
            verificationInvocationsContainer.ExpectUnverifiedInvocation(invocationDescriptor);
         }
         return new Expectation<TOut1, TResult>(invocationDescriptor, invocationOperationManagerFinder);
      }

      public Expectation<TOut1, TOut2, TResult> Create<TOut1, TOut2, TResult>(Func<TOut1, TOut2, TResult> func, bool expectInvocation) {
         func(default(TOut1), default(TOut2));
         var invocationDescriptor = invocationStage.ReleaseLastInvocation();
         if (expectInvocation) {
            verificationInvocationsContainer.ExpectUnverifiedInvocation(invocationDescriptor);
         }
         return new Expectation<TOut1, TOut2, TResult>(invocationDescriptor, invocationOperationManagerFinder);
      }

      public Expectation<TOut1, TOut2, TOut3, TResult> Create<TOut1, TOut2, TOut3, TResult>(Func<TOut1, TOut2, TOut3, TResult> func, bool expectInvocation) {
         func(default(TOut1), default(TOut2), default(TOut3));
         var invocationDescriptor = invocationStage.ReleaseLastInvocation();
         if (expectInvocation) {
            verificationInvocationsContainer.ExpectUnverifiedInvocation(invocationDescriptor);
         }
         return new Expectation<TOut1, TOut2, TOut3, TResult>(invocationDescriptor, invocationOperationManagerFinder);
      }
   }
}
