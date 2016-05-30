using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Courier.TransportTier.Udp.Vox;
using Dargon.Commons;

namespace Dargon.Courier.TransportTier.Udp {
   public class PacketSender {
      private readonly PayloadSender payloadSender;
      private readonly AcknowledgementCoordinator acknowledgementCoordinator;
      private readonly CancellationToken shutdownCancellationToken;

      public PacketSender(PayloadSender payloadSender, AcknowledgementCoordinator acknowledgementCoordinator, CancellationToken shutdownCancellationToken) {
         this.payloadSender = payloadSender;
         this.acknowledgementCoordinator = acknowledgementCoordinator;
         this.shutdownCancellationToken = shutdownCancellationToken;
      }

      public async Task SendAsync(PacketDto x) {
         await Task.Yield();

         if (!x.IsReliable()) {
            await payloadSender.SendAsync(x);
         } else {
            using (var acknowledgedCts = new CancellationTokenSource())
            using (var acknowledgedOrShutdownCts = CancellationTokenSource.CreateLinkedTokenSource(acknowledgedCts.Token, shutdownCancellationToken)) {
               var expectation = acknowledgementCoordinator.Expect(x.Id, shutdownCancellationToken);
               expectation.ContinueWith(state => {
                  acknowledgedCts.Cancel();
               }, shutdownCancellationToken).Forget();

               const int resendDelay = 1000;
               while (!expectation.IsCompleted && !shutdownCancellationToken.IsCancellationRequested) {
                  try {
                     await payloadSender.SendAsync(x);
                     await Task.Delay(resendDelay, acknowledgedCts.Token);
                  } catch (TaskCanceledException) {
                     // It's on the Task.Delay
                  }
               }
            }
         }
      }
   }
}
