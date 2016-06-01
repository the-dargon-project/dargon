using System;
using System.Threading;
using Dargon.Commons;
using Dargon.Courier.TransportTier.Udp.Vox;
using System.Threading.Tasks;

namespace Dargon.Courier.TransportTier.Udp {
   public class Announcer {
      private const int kAnnounceIntervalMillis = 500;
      private readonly Identity identity;
      private readonly PayloadSender payloadSender;
      private readonly CancellationToken shutdownCancellationToken;

      public Announcer(Identity identity, PayloadSender payloadSender, CancellationToken shutdownCancellationToken) {
         this.identity = identity;
         this.payloadSender = payloadSender;
         this.shutdownCancellationToken = shutdownCancellationToken;
      }

      public void Initialize() {
         RunAnnounceLoopAsync().Forget();
      }

      private async Task RunAnnounceLoopAsync() {
         await Task.Yield();

         var announce = new AnnouncementDto { Identity = identity };

         while (!shutdownCancellationToken.IsCancellationRequested) {
            try {
               await payloadSender.SendAsync(announce);
               await Task.Delay(kAnnounceIntervalMillis, shutdownCancellationToken);
            } catch (OperationCanceledException) {
               // shutdown cancellation token cancelled
            }
         }
      }
   }
}
