using System;
using System.Linq;
using NMockito.Counters;
using NMockito.Mocks;
using NMockito.Utilities;

namespace NMockito.Verification {
   public class VerificationOperations {
      private readonly InvocationStage invocationStage;
      private readonly VerificationInvocationsContainer verificationInvocationsContainer;

      public VerificationOperations(InvocationStage invocationStage, VerificationInvocationsContainer verificationInvocationsContainer) {
         this.invocationStage = invocationStage;
         this.verificationInvocationsContainer = verificationInvocationsContainer;
      }

      public void VerifyExpectationsAndNoMoreInteractions() {
         Exception e = null;
         try {
            VerifyExpectations();
         } catch (Exception ex) {
            e = ex;
         }
         try {
            VerifyNoMoreInteractions();
         } catch (Exception ex) {
            if (e == null) {
               e = ex;
            } else {
               e = new AggregateException(e, ex);
            }
         }
         e?.Rethrow();
      }

      public void VerifyExpectations() {
         invocationStage.FlushUnverifiedInvocation();
         var expectedInvocations = verificationInvocationsContainer.ExpectedInvocationDescriptors;
         foreach (var expectedInvocationKvp in expectedInvocations) {
            var expectedInvocation = expectedInvocationKvp.Key;
            var expectedInvocationCounter = expectedInvocationKvp.Value;

            VerifyInvocation(expectedInvocation, expectedInvocationCounter);

            if (expectedInvocationCounter.IsSatisfied) {
               expectedInvocations.TryRemove(expectedInvocation, out expectedInvocationCounter);
            }
         }
         if (expectedInvocations.Any(x => !x.Value.IsSatisfied)) {
            throw new InvocationExpectationException("Expected but did not find mock invocations:", expectedInvocations);
         }
      }

      public void VerifyInvocation(InvocationDescriptor expectedInvocation, Counter expectedInvocationCounter) {
         var unverifiedInvocations = verificationInvocationsContainer.UnverifiedInvocationDescriptors;

         foreach (var unverifiedInvocationKvp in unverifiedInvocations) {
            var unverifiedInvocation = unverifiedInvocationKvp.Key;
            var unverifiedInvocationCounter = unverifiedInvocationKvp.Value;
            if (expectedInvocation.Mock == unverifiedInvocation.Mock &&
                Equals(expectedInvocation.Method, unverifiedInvocation.Method) &&
                expectedInvocation.SmartParameters.Matches(unverifiedInvocation)) {
               var countsRemoved = Math.Min(expectedInvocationCounter.Remaining, unverifiedInvocationCounter.Remaining);
               expectedInvocationCounter.HandleVerified(countsRemoved);
               unverifiedInvocationCounter.HandleVerified(countsRemoved);
            }
            if (unverifiedInvocationCounter.IsSatisfied) {
               unverifiedInvocations.TryRemove(unverifiedInvocation, out unverifiedInvocationCounter);
            }
         }
      }

      public void VerifyNoMoreInteractions() {
         invocationStage.FlushUnverifiedInvocation();
         var unverifiedInvocations = verificationInvocationsContainer.UnverifiedInvocationDescriptors;
         if (unverifiedInvocations.Any()) {
            throw new InvocationExpectationException("Expected no more mock invocations but found:", unverifiedInvocations);
         }
      }
   }
}