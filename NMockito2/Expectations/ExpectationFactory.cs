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

      public Expectation When(Action action) {
         action();
         return When();
      }

      public Expectation When() {
         var invocationDescriptor = invocationStage.ReleaseLastInvocation();
         return new Expectation(invocationDescriptor, invocationOperationManagerFinder);
      }

      public Expectation<T> When<T>() {
         var invocationDescriptor = invocationStage.ReleaseLastInvocation();
         return new Expectation<T>(invocationDescriptor, invocationOperationManagerFinder);
      }

      public Expectation<TOut1, TResult> When<TOut1, TResult>(Func<TOut1, TResult> func) {
         func(default(TOut1));
         var invocationDescriptor = invocationStage.ReleaseLastInvocation();
         return new Expectation<TOut1, TResult>(invocationDescriptor, invocationOperationManagerFinder);
      }

      public Expectation<TOut1, TOut2, TResult> When<TOut1, TOut2, TResult>(Func<TOut1, TOut2, TResult> func) {
         func(default(TOut1), default(TOut2));
         var invocationDescriptor = invocationStage.ReleaseLastInvocation();
         return new Expectation<TOut1, TOut2, TResult>(invocationDescriptor, invocationOperationManagerFinder);
      }

      public Expectation<TOut1, TOut2, TOut3, TResult> When<TOut1, TOut2, TOut3, TResult>(Func<TOut1, TOut2, TOut3, TResult> func) {
         func(default(TOut1), default(TOut2), default(TOut3));
         var invocationDescriptor = invocationStage.ReleaseLastInvocation();
         return new Expectation<TOut1, TOut2, TOut3, TResult>(invocationDescriptor, invocationOperationManagerFinder);
      }

      public Expectation Expect(Action action) {
         action();
         return Expect();
      }

      public Expectation Expect() {
         var invocationDescriptor = invocationStage.ReleaseLastInvocation();
         verificationInvocationsContainer.ExpectUnverifiedInvocation(invocationDescriptor);
         return new Expectation(invocationDescriptor, invocationOperationManagerFinder);
      }

      public Expectation<TResult> Expect<TResult>() {
         var invocationDescriptor = invocationStage.ReleaseLastInvocation();
         verificationInvocationsContainer.ExpectUnverifiedInvocation(invocationDescriptor);
         return new Expectation<TResult>(invocationDescriptor, invocationOperationManagerFinder);
      }

      public Expectation<TOut1, TResult> Expect<TResult, TOut1>(Func<TOut1, TResult> func) {
         func(default(TOut1));
         var invocationDescriptor = invocationStage.ReleaseLastInvocation();
         verificationInvocationsContainer.ExpectUnverifiedInvocation(invocationDescriptor);
         return new Expectation<TOut1, TResult>(invocationDescriptor, invocationOperationManagerFinder);
      }

      public Expectation<TOut1, TOut2, TResult> Expect<TOut1, TOut2, TResult>(Func<TOut1, TOut2, TResult> func) {
         func(default(TOut1), default(TOut2));
         var invocationDescriptor = invocationStage.ReleaseLastInvocation();
         verificationInvocationsContainer.ExpectUnverifiedInvocation(invocationDescriptor);
         return new Expectation<TOut1, TOut2, TResult>(invocationDescriptor, invocationOperationManagerFinder);
      }

      public Expectation<TOut1, TOut2, TOut3, TResult> Expect<TOut1, TOut2, TOut3, TResult>(Func<TOut1, TOut2, TOut3, TResult> func) {
         func(default(TOut1), default(TOut2), default(TOut3));
         var invocationDescriptor = invocationStage.ReleaseLastInvocation();
         verificationInvocationsContainer.ExpectUnverifiedInvocation(invocationDescriptor);
         return new Expectation<TOut1, TOut2, TOut3, TResult>(invocationDescriptor, invocationOperationManagerFinder);
      }
   }
}
