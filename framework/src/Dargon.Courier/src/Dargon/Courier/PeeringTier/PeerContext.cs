using Dargon.Commons;
using Dargon.Commons.AsyncPrimitives;
using NLog;
using System;
using System.Threading;
using System.Threading.Tasks;
using static Dargon.Commons.Channels.ChannelsExtensions;

namespace Dargon.Courier.PeeringTier {
   public class PeerContext {
      private static readonly Logger logger = LogManager.GetCurrentClassLogger();
      private readonly AsyncLock synchronization = new AsyncLock();
      private readonly AsyncLatch discoveryLatch = new AsyncLatch();
      private readonly PeerTable peerTable;
      private readonly IAsyncPoster<PeerDiscoveryEvent> peerDiscoveryEventPoster;

      private const int kNotDiscovered = 0;
      private const int kDiscovered = 1;
      private int discoveryState = kNotDiscovered;

      public PeerContext(PeerTable peerTable, Guid peerId, IAsyncPoster<PeerDiscoveryEvent> peerDiscoveryEventPoster) {
         this.peerTable = peerTable;
         this.peerDiscoveryEventPoster = peerDiscoveryEventPoster;
         this.Identity = new Identity(peerId);
      }

      public PeerTable PeerTable => peerTable;
      public bool Discovered { get; private set; }
      public Identity Identity { get; }

      public Task WaitForDiscoveryAsync(CancellationToken cancellationToken = default(CancellationToken)) {
         return discoveryLatch.WaitAsync(cancellationToken);
      }

      public void HandleInboundPeerIdentityUpdate(Identity identity) {
//         logger.Trace($"Got announcement from peer {identity}!");
         Identity.Update(identity);

         if (Interlocked.CompareExchange(ref discoveryState, kDiscovered, kNotDiscovered) == kNotDiscovered) {
            Go(async () => {
               var discoveryEvent = new PeerDiscoveryEvent { Peer = this };
               await peerDiscoveryEventPoster.PostAsync(discoveryEvent).ConfigureAwait(false);
               discoveryLatch.SetOrThrow();
            }).Forget();
         }
      }
   }
}