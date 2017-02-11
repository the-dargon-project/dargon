using System.Threading;
using Dargon.Courier.PeeringTier;
using Dargon.Courier.RoutingTier;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Commons.AsyncPrimitives;
using Dargon.Commons.Pooling;
using Dargon.Courier.AuditingTier;
using Dargon.Courier.ManagementTier;
using Dargon.Courier.TransportTier.Udp.Management;
using Dargon.Ryu;

namespace Dargon.Courier.TransportTier.Udp {
   public class UdpTransportFactory : ITransportFactory {
      private readonly UdpTransportConfiguration configuration;

      public UdpTransportFactory(UdpTransportConfiguration configuration = null) {
         this.configuration = configuration ?? UdpTransportConfiguration.Default;
      }

      public Task<ITransport> CreateAsync(MobOperations mobOperations, Identity identity, RoutingTable routingTable, PeerTable peerTable, InboundMessageDispatcher inboundMessageDispatcher, AuditService auditService) {
         // setup identity
         identity.Properties[UdpConstants.kUnicastPortIdentityPropertyKey] = configuration.UnicastReceiveEndpoint.Port.ToString();

         var duplicateFilter = new DuplicateFilter();
         duplicateFilter.Initialize();

         var inboundBytesAggregator = auditService.GetAggregator<double>(DataSetNames.kInboundBytes);
         var outboundBytesAggregator = auditService.GetAggregator<double>(DataSetNames.kOutboundBytes);
         var inboundReceiveProcessDispatchLatencyAggregator = auditService.GetAggregator<double>(DataSetNames.kInboundProcessDispatchLatency);
         var resendsCounter = auditService.GetCounter(DataSetNames.kTotalResends);
         var resendsAggregator = auditService.GetAggregator<int>(DataSetNames.kMessageResends);
         var tossedCounter = auditService.GetCounter(DataSetNames.kTossed);
         var duplicatesReceivedCounter = auditService.GetCounter(DataSetNames.kDuplicatesReceived);
         var announcementsReceivedCounter = auditService.GetCounter(DataSetNames.kAnnouncementsReceived);
         var multiPartChunksSentCounter = auditService.GetCounter(DataSetNames.kMultiPartChunksSent);
         var multiPartChunksReceivedAggregator = auditService.GetAggregator<int>(DataSetNames.kMultiPartChunksBytesReceived);
         var outboundMessageRateLimitAggregator = auditService.GetAggregator<double>(DataSetNames.kOutboundMessageRateLimit);
         var sendQueueDepthAggregator = auditService.GetAggregator<double>(DataSetNames.kSendQueueDepth);

         mobOperations.RegisterMob(new UdpDebugMob());

         var shutdownCts = new CancellationTokenSource();
         var acknowledgementCoordinator = new AcknowledgementCoordinator(identity);
         var udpUnicastScheduler = SchedulerFactory.CreateWithCustomThreadPool($"Courier.Udp({identity.Id.ToShortString()}).Unicast");
         var sendReceiveBufferPool = ObjectPool.CreateStackBacked(() => new byte[UdpConstants.kMaximumTransportSize]);
         var client = UdpClient.Create(configuration, udpUnicastScheduler, sendReceiveBufferPool, inboundBytesAggregator, outboundBytesAggregator, inboundReceiveProcessDispatchLatencyAggregator);
         var payloadSender = new PayloadSender(client);
         var multiPartPacketReassembler = new MultiPartPacketReassembler();
         var udpUnicasterFactory = new UdpUnicasterFactory(identity, client, acknowledgementCoordinator, sendReceiveBufferPool, resendsCounter, resendsAggregator, outboundMessageRateLimitAggregator, sendQueueDepthAggregator);
         var udpDispatcher = new UdpDispatcherImpl(identity, client, duplicateFilter, payloadSender, acknowledgementCoordinator, routingTable, peerTable, inboundMessageDispatcher, multiPartPacketReassembler, udpUnicasterFactory, announcementsReceivedCounter, tossedCounter, duplicatesReceivedCounter, multiPartChunksReceivedAggregator);
         multiPartPacketReassembler.SetUdpDispatcher(udpDispatcher);
         var announcer = new Announcer(identity, payloadSender, shutdownCts.Token);
         announcer.Initialize();
         var udpFacade = new UdpFacade(client, udpDispatcher, shutdownCts);
         var udpBroadcaster = new UdpBroadcaster(identity, client);
         var transport = new UdpTransport(udpBroadcaster, udpFacade);
         client.StartReceiving(udpDispatcher);

         return Task.FromResult<ITransport>(transport);
      }
   }
}
