using System.Threading;
using Dargon.Courier.PeeringTier;
using Dargon.Courier.RoutingTier;
using System.Threading.Tasks;

namespace Dargon.Courier.TransportTier.Udp {
   public class UdpTransportFactory : ITransportFactory {
      private readonly UdpTransportConfiguration configuration;

      public UdpTransportFactory(UdpTransportConfiguration configuration = null) {
         this.configuration = configuration ?? UdpTransportConfiguration.Default;
      }

      public Task<ITransport> CreateAsync(Identity identity, RoutingTable routingTable, PeerTable peerTable, InboundMessageDispatcher inboundMessageDispatcher) {
         var duplicateFilter = new DuplicateFilter();
         duplicateFilter.Initialize();

         var shutdownCts = new CancellationTokenSource();

         var acknowledgementCoordinator = new AcknowledgementCoordinator();
         var client = UdpClient.Create(configuration);
         var payloadSender = new PayloadSender(client);
         var packetSender = new PacketSender(payloadSender, acknowledgementCoordinator, shutdownCts.Token);
         var messageSender = new MessageSender(identity, packetSender);
         var udpDispatcher = new UdpDispatcher(identity, client, messageSender, duplicateFilter, payloadSender, acknowledgementCoordinator, routingTable, peerTable, inboundMessageDispatcher);
         var announcer = new Announcer(identity, payloadSender, shutdownCts.Token);
         announcer.Initialize();
         var udpFacade = new UdpFacade(client, udpDispatcher, shutdownCts);
         var transport = new UdpTransport(messageSender, udpFacade);
         client.StartReceiving(udpDispatcher);

         return Task.FromResult<ITransport>(transport);
      }
   }
}
