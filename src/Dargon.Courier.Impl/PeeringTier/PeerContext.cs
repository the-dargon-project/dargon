using Dargon.Courier.AsyncPrimitives;
using Dargon.Courier.TransportTier.Udp.Vox;
using Nito.AsyncEx;
using NLog;
using System.Threading;
using System.Threading.Tasks;

namespace Dargon.Courier.PeeringTier {
   public class PeerContext {
      private static readonly Logger logger = LogManager.GetCurrentClassLogger();
      private readonly AsyncLock synchronization = new AsyncLock();
      private readonly AsyncLatch discoveryLatch = new AsyncLatch();
      private readonly PeerTable peerTable;
      private readonly IAsyncPoster<PeerDiscoveryEvent> peerDiscoveryEventPoster;

      public PeerContext(PeerTable peerTable, IAsyncPoster<PeerDiscoveryEvent> peerDiscoveryEventPoster) {
         this.peerTable = peerTable;
         this.peerDiscoveryEventPoster = peerDiscoveryEventPoster;
      }

      public PeerTable PeerTable => peerTable;
      public bool Discovered { get; private set; }
      public Identity Identity { get; } = new Identity();

      public Task WaitForDiscoveryAsync(CancellationToken cancellationToken = default(CancellationToken)) {
         return discoveryLatch.WaitAsync(cancellationToken);
      }

      public async Task HandleInboundPeerIdentityUpdate(Identity identity) {
         await Task.Yield();

//         logger.Trace($"Got announcement from peer {identity}!");
         Identity.Update(identity);

         if (!Discovered) {
            using (await synchronization.LockAsync()) {
               if (!Discovered) {
                  Discovered = true;
                  var discoveryEvent = new PeerDiscoveryEvent { Peer = this };
                  logger.Info("__A");
                  await peerDiscoveryEventPoster.PostAsync(discoveryEvent);
                  logger.Info("__B");
                  discoveryLatch.Set();
                  logger.Info("__C");
               }
            }
         }
      }
   }
}