using System.Threading;
using Dargon.Courier.PeeringTier;
using Dargon.Courier.RoutingTier;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Commons.AsyncPrimitives;
using Dargon.Commons.Pooling;
using Dargon.Commons.Scheduler;
using Dargon.Courier.AccessControlTier;
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

      public ITransport Create(CourierFacade courier) {
         var auditService = courier.AuditService;
         var identity = courier.Identity;
         var synchronizationContexts = courier.SynchronizationContexts;

         // Setup identity
         courier.Identity.DeclaredProperties[UdpConstants.kUnicastPortIdentityPropertyKey] = configuration.UnicastReceiveEndpoint.Port.ToString();

         // Analytics
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
         
         // Management Objects
         var mobOperations = courier.MobOperations;
         mobOperations.RegisterMob(new UdpDebugMob());

         var acknowledgementCoordinator = new AcknowledgementCompletionLatchContainer(identity);
         // var udpUnicastScheduler = schedulerFactory.CreateWithCustomThreadPool($"Courier.Udp({identity.Id.ToString()})");
         // var sendReceiveBufferPool = ObjectPool.CreateConcurrentQueueBacked(() => new byte[UdpConstants.kMaximumTransportSize]);
         // var client = UdpClient.Create(configuration, udpUnicastScheduler, sendReceiveBufferPool, inboundBytesAggregator, outboundBytesAggregator, inboundReceiveProcessDispatchLatencyAggregator);

         // Low-level UDP send/receive, NIC detection
         var shutdownCts = new CancellationTokenSource();
         var schedulerFactory = new SchedulerFactory(new ThreadFactory());
         var coreUdp = new CoreUdp(configuration, synchronizationContexts);

         // Sends
         var payloadSender = new CoreBroadcaster(coreUdp);
         var announcer = new Announcer(identity, payloadSender, shutdownCts.Token);

         // Receives
         var multiPartPacketReassembler = new MultiPartPacketReassembler();
         var duplicateFilter = new DuplicateFilter();
         var udpDispatcher = new UdpDispatcher(identity, duplicateFilter, payloadSender, acknowledgementCoordinator, courier.RoutingTable, courier.PeerTable, courier.InboundMessageDispatcher, multiPartPacketReassembler, announcementsReceivedCounter, tossedCounter, duplicatesReceivedCounter, multiPartChunksReceivedAggregator, courier.Gatekeeper, courier.SynchronizationContexts);
         udpDispatcher.SetCreateUdpUnicasterFunc(remoteInfo => new(identity, coreUdp, acknowledgementCoordinator, remoteInfo, resendsCounter, resendsAggregator, outboundMessageRateLimitAggregator, sendQueueDepthAggregator));
         multiPartPacketReassembler.SetUdpDispatcher(udpDispatcher);
         coreUdp.AddReceiveListener(udpDispatcher);
         
         // Boot
         announcer.Initialize();
         
         // 
         var udpFacade = new UdpFacade(coreUdp, udpDispatcher, shutdownCts);
         var udpBroadcaster = new UdpBroadcaster(identity, coreUdp);
         var transport = new UdpTransport(udpBroadcaster, udpFacade);
         client.StartReceiving(udpDispatcher);

         return transport;
      }
   }
}
