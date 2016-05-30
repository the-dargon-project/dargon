using System.Threading;
using System.Threading.Tasks;
using Dargon.Courier.AsyncPrimitives;
using Dargon.Courier.Vox;
using Nito.AsyncEx;
using NLog;

namespace Dargon.Courier.PeeringTier {
   public class PeerContext : AsyncProcessor<InboundPayloadEvent, InboundPayloadEvent> {
      private static readonly Logger logger = LogManager.GetCurrentClassLogger();
      private readonly AsyncLock synchronization = new AsyncLock();
      private readonly AsyncLatch discoveryLatch = new AsyncLatch();
      private readonly PeerTable peerTable;
      private readonly IAsyncPoster<PeerDiscoveryEvent> peerDiscoveryEventPoster;

      public PeerContext(PeerTable peerTable, IAsyncPoster<PeerDiscoveryEvent> peerDiscoveryEventPoster, AsyncRouter<InboundPayloadEvent, InboundPayloadEvent> router) : base(router) {
         this.peerTable = peerTable;
         this.peerDiscoveryEventPoster = peerDiscoveryEventPoster;
         router.RegisterHandler<AnnouncementDto>(HandleAnnouncementAsync);
      }

      public PeerTable PeerTable => peerTable;
      public bool Discovered { get; private set; }
      public Identity Identity { get; } = new Identity();

      public Task WaitForDiscoveryAsync(CancellationToken cancellationToken = default(CancellationToken)) {
         return discoveryLatch.WaitAsync(cancellationToken);
      }

      public async Task HandleAnnouncementAsync(InboundPayloadEvent e) {
         var announcement = (AnnouncementDto)e.Payload;
         logger.Trace($"Got announcement from peer {announcement.Identity}!");
         Identity.Update(announcement.Identity);
         if (!Discovered) {
            using (await synchronization.LockAsync()) {
               if (!Discovered) {
                  Discovered = true;
                  var discoveryEvent = new PeerDiscoveryEvent {
                     Peer = this,
                     PayloadEvent = e,
                     Announcement = announcement
                  };
                  await peerDiscoveryEventPoster.PostAsync(discoveryEvent);
                  discoveryLatch.Set();
               }
            }
         }
      }
   }
}