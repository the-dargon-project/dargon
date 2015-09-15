using System.Threading;
using NMockito2.Verification;

namespace NMockito2.Mocks {
   public class InvocationStage {
      private readonly object synchronization = new object();
      private readonly VerificationInvocationsContainer verificationInvocationsContainer;
      private InvocationDescriptor stagedInvocationDescriptor;

      public InvocationStage(VerificationInvocationsContainer verificationInvocationsContainer) {
         this.verificationInvocationsContainer = verificationInvocationsContainer;
      }

      public void SetLastInvocation(InvocationDescriptor invocationDescriptor) {
         InvocationDescriptor lastStagedInvocationDescriptor;
         SpinWait spinner = new SpinWait();
         while (!TrySwapStagedInvocationDescriptor(invocationDescriptor, out lastStagedInvocationDescriptor)) {
            spinner.SpinOnce();
         }

         if (lastStagedInvocationDescriptor != null) {
            verificationInvocationsContainer.HandleUnverifiedInvocation(lastStagedInvocationDescriptor);
         }
      }

      private bool TrySwapStagedInvocationDescriptor(InvocationDescriptor invocationDescriptor, out InvocationDescriptor lastStagedInvocationDescriptor) {
         lastStagedInvocationDescriptor = stagedInvocationDescriptor;
         var originalStagedInvocationDescriptor = Interlocked.CompareExchange(
            ref stagedInvocationDescriptor,
            invocationDescriptor,
            lastStagedInvocationDescriptor);
         return originalStagedInvocationDescriptor == lastStagedInvocationDescriptor;
      }

      public InvocationDescriptor GetLastInvocation() {
         return stagedInvocationDescriptor;
      }

      public InvocationDescriptor ReleaseLastInvocation() {
         var result = stagedInvocationDescriptor;
         stagedInvocationDescriptor = null;
         return result;
      }

      public void FlushUnverifiedInvocation() {
         SetLastInvocation(null);
      }
   }
}