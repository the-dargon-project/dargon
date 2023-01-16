using System;
using System.Threading;
using Dargon.Commons;
using Dargon.Courier.TransportTier.Udp.Vox;
using System.Threading.Tasks;

namespace Dargon.Courier.TransportTier.Udp {
   public class Announcer {
      private const int kAnnounceIntervalMillis = 5000;
      private readonly Identity identity;
      private readonly CoreBroadcaster coreBroadcaster;
      private readonly CancellationToken shutdownCancellationToken;

      public Announcer(Identity identity, CoreBroadcaster coreBroadcaster, CancellationToken shutdownCancellationToken) {
         this.identity = identity;
         this.coreBroadcaster = coreBroadcaster;
         this.shutdownCancellationToken = shutdownCancellationToken;
      }

      public void Initialize() {
         RunAnnounceLoopAsync().Forget();
      }

      private async Task RunAnnounceLoopAsync() {
         //await TaskEx.YieldToThreadPool();

         var announce = new AnnouncementDto { Identity = identity };

         while (!shutdownCancellationToken.IsCancellationRequested) {
            try {
               await coreBroadcaster.BroadcastAsync(announce).ConfigureAwait(false);
               await Task.Delay(kAnnounceIntervalMillis, shutdownCancellationToken).ConfigureAwait(false);
            } catch (OperationCanceledException) {
               // shutdown cancellation token cancelled
            }
         }
      }
   }
}
