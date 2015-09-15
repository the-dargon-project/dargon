using NMockito2.Mocks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using NMockito2.Counters;
using NMockito2.Expectations;
using NMockito2.Utilities;

namespace NMockito2.Verification {
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
         var unverifiedInvocations = verificationInvocationsContainer.UnverifiedInvocationDescriptors;
         foreach (var expectedInvocationKvp in expectedInvocations) {
            var expectedInvocation = expectedInvocationKvp.Key;
            var expectedInvocationCounter = expectedInvocationKvp.Value;
            foreach (var unverifiedInvocationKvp in unverifiedInvocations) {
               var unverifiedInvocation = unverifiedInvocationKvp.Key;
               var unverifiedInvocationCounter = unverifiedInvocationKvp.Value;
               if (expectedInvocation.SmartParameters.Matches(unverifiedInvocation)) {
                  var countsRemoved = Math.Min(expectedInvocationCounter.Remaining, unverifiedInvocationCounter.Remaining);
                  expectedInvocationCounter.HandleVerified(countsRemoved);
                  unverifiedInvocationCounter.HandleVerified(countsRemoved);
               }
               if (unverifiedInvocationCounter.IsSatisfied) {
                  unverifiedInvocations.TryRemove(unverifiedInvocation, out unverifiedInvocationCounter);
               }
            }
            if (expectedInvocationCounter.IsSatisfied) {
               expectedInvocations.TryRemove(expectedInvocation, out expectedInvocationCounter);
            }
         }
         if (expectedInvocations.Any(x => !x.Value.IsSatisfied)) {
            throw new UnexpectedInvocationsException("Expected but did not find mock invocations:", expectedInvocations);
         }
      }

      public void VerifyNoMoreInteractions() {
         invocationStage.FlushUnverifiedInvocation();
         var unverifiedInvocations = verificationInvocationsContainer.UnverifiedInvocationDescriptors;
         if (unverifiedInvocations.Any()) {
            throw new UnexpectedInvocationsException("Expected no more mock invocations but found:", unverifiedInvocations);
         }
      }
   }

   public class UnexpectedInvocationsException : Exception {
      public UnexpectedInvocationsException(string message, ConcurrentDictionary<InvocationDescriptor, Counter> invocationsAndCounters) : base(GenerateMessage(message, invocationsAndCounters)) {

      }

      private static string GenerateMessage(string message, ConcurrentDictionary<InvocationDescriptor, Counter> invocationsAndCounters) {
         var sb = new StringBuilder();
         sb.AppendLine(message);
         foreach (var invocation in invocationsAndCounters) {
            sb.AppendLine(invocation.Value.Description + " invocations of: " + invocation.Key);
         }
         return sb.ToString();
      }
   }

   public class VerificationInvocationsContainer {
      private ConcurrentDictionary<InvocationDescriptor, Counter> expectedInvocationDescriptors = new ConcurrentDictionary<InvocationDescriptor, Counter>();
      private ConcurrentDictionary<InvocationDescriptor, Counter> unverifiedInvocationDescriptors = new ConcurrentDictionary<InvocationDescriptor, Counter>();

      public void ExpectUnverifiedInvocation(InvocationDescriptor invocationDescriptor) {
         expectedInvocationDescriptors.AddOrUpdate(
            invocationDescriptor, 
            add => new AnyCounter(), 
            (update, existing) => existing);
      }

      public void HandleUnverifiedInvocation(InvocationDescriptor invocationDescriptor) {
         unverifiedInvocationDescriptors.AddOrUpdate(
            invocationDescriptor, 
            add => new TimesCounter(1),
            (update, existing) => new TimesCounter(existing.Remaining + 1));
      }

      public ConcurrentDictionary<InvocationDescriptor, Counter> ExpectedInvocationDescriptors => expectedInvocationDescriptors;
      public ConcurrentDictionary<InvocationDescriptor, Counter> UnverifiedInvocationDescriptors => unverifiedInvocationDescriptors;
   }
}
