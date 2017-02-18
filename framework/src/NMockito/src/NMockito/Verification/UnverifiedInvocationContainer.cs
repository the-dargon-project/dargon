using System;
using System.Collections.Concurrent;
using System.Text;
using NMockito.Counters;
using NMockito.Mocks;

namespace NMockito.Verification {
   public class InvocationExpectationException : Exception {
      public InvocationExpectationException(string message, ConcurrentDictionary<InvocationDescriptor, Counter> invocationsAndCounters) : base(GenerateMessage(message, invocationsAndCounters)) {

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
