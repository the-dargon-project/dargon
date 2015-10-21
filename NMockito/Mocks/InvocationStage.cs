using System.Threading;
using NMockito.Verification;

namespace NMockito.Mocks {
   public class InvocationStage {
      private readonly object synchronization = new object();
      private readonly VerificationInvocationsContainer verificationInvocationsContainer;
      private InvocationDescriptor stagedInvocationDescriptor;

      public InvocationStage(VerificationInvocationsContainer verificationInvocationsContainer) {
         this.verificationInvocationsContainer = verificationInvocationsContainer;
      }

      public void SetLastInvocation(InvocationDescriptor invocationDescriptor) {
         var lastStagedInvocationDescriptor = SwapLastStagedInvocation(invocationDescriptor);

         if (lastStagedInvocationDescriptor != null &&
             lastStagedInvocationDescriptor.Interceptor.IsTracked) {
            verificationInvocationsContainer.HandleUnverifiedInvocation(lastStagedInvocationDescriptor);
         }
      }

      private InvocationDescriptor SwapLastStagedInvocation(InvocationDescriptor invocationDescriptor) {
         InvocationDescriptor lastStagedInvocationDescriptor;
         SpinWait spinner = new SpinWait();
         while (!TrySwapStagedInvocationDescriptor(invocationDescriptor, out lastStagedInvocationDescriptor)) {
            spinner.SpinOnce();
         }
         return lastStagedInvocationDescriptor;
      }

      private bool TrySwapStagedInvocationDescriptor(InvocationDescriptor invocationDescriptor, out InvocationDescriptor lastStagedInvocationDescriptor) {
         lastStagedInvocationDescriptor = stagedInvocationDescriptor;
         var originalStagedInvocationDescriptor = Interlocked.CompareExchange(
            ref stagedInvocationDescriptor,
            invocationDescriptor,
            lastStagedInvocationDescriptor);
         return originalStagedInvocationDescriptor == lastStagedInvocationDescriptor;
      }

      public InvocationDescriptor ReleaseLastInvocation() {
         return SwapLastStagedInvocation(null);
      }

      public void FlushUnverifiedInvocation() {
         SetLastInvocation(null);
      }
   }
}