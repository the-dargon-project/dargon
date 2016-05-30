using Dargon.Commons;
using Dargon.Courier.Vox;
using System.Threading.Tasks;

namespace Dargon.Courier.PacketTier {
   public class Announcer {
      private const int kAnnounceIntervalMillis = 100;
      private readonly Identity identity;
      private readonly OutboundPayloadEventEmitter outboundPayloadEventEmitter;

      public Announcer(Identity identity, OutboundPayloadEventEmitter outboundPayloadEventEmitter) {
         this.identity = identity;
         this.outboundPayloadEventEmitter = outboundPayloadEventEmitter;
      }

      public void Initialize() {
         RunAnnounceLoopAsync().Forget();
      }

      private async Task RunAnnounceLoopAsync() {
         await Task.Yield();

         var announce = new AnnouncementDto();
         announce.Identity = identity;

         while (true) {
            await outboundPayloadEventEmitter.EmitAsync(announce, null);
            await Task.Delay(kAnnounceIntervalMillis);
         }
      }
   }
}