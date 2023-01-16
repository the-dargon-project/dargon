using System;
using System.Collections.Generic;
using Dargon.Courier.AuditingTier;
using Dargon.Courier.ManagementTier;
using Dargon.Courier.PeeringTier;
using Dargon.Courier.RoutingTier;
using Dargon.Courier.TransportTier.Tcp.Management;
using Dargon.Courier.TransportTier.Tcp.Server;
using System.Net;
using System.Threading.Tasks;
using Dargon.Courier.AccessControlTier;

namespace Dargon.Courier.TransportTier.Tcp {
   public class TcpTransportConnectionFailureEventArgs {
      public TcpTransportConnectionFailureEventArgs(Exception e) {
         Exception = e;
      }

      public Exception Exception { get; }
   }

   public delegate void TcpTransportConnectionFailureHandler(TcpTransportConnectionFailureEventArgs e);

   public class TcpTransportHandshakeCompletionEventArgs {
      public TcpTransportHandshakeCompletionEventArgs(Identity remoteIdentity) {
         RemoteIdentity = remoteIdentity;
      }

      public Identity RemoteIdentity { get; }
   }

   public delegate void TcpTransportHandshakeCompletionHandler(TcpTransportHandshakeCompletionEventArgs e);

   public class TcpTransportConfiguration {
      public TcpTransportConfiguration(IPEndPoint remoteEndpoint, TcpRole role) {
         RemoteEndpoint = remoteEndpoint;
         Role = role;
      }

      public IPEndPoint RemoteEndpoint { get; }
      public TcpRole Role { get; }
      public Dictionary<string, object> AdditionalHandshakeParameters { get; }

      public event TcpTransportConnectionFailureHandler ConnectionFailure;
      public event TcpTransportHandshakeCompletionHandler HandshakeCompleted;

      internal void HandleRemoteHandshakeCompletion(Identity remoteIdentity) {
         HandshakeCompleted?.Invoke(new TcpTransportHandshakeCompletionEventArgs(remoteIdentity));
      }

      internal void HandleConnectionFailure(Exception e) {
         Console.WriteLine("HCF " + e);
         ConnectionFailure?.Invoke(new TcpTransportConnectionFailureEventArgs(e));
      }
   }

   public class TcpTransportFactory : ITransportFactory {
      private TcpTransportConfiguration configuration;

      public TcpTransportFactory(TcpTransportConfiguration configuration) {
         this.configuration = configuration;
      }

      public ITransport Create(MobOperations mobOperations, Identity identity, RoutingTable routingTable, PeerTable peerTable, InboundMessageDispatcher inboundMessageDispatcher, AuditService auditService, IGatekeeper gatekeeper) {
         var inboundBytesAggregator = auditService.GetAggregator<double>(DataSetNames.kInboundBytes);
         var outboundBytesAggregator = auditService.GetAggregator<double>(DataSetNames.kOutboundBytes);

         var tcpRoutingContextContainer = new TcpRoutingContextContainer();
         var payloadUtils = new PayloadUtils(inboundBytesAggregator, outboundBytesAggregator);
         var transport = new TcpTransport(configuration, identity, routingTable, peerTable, inboundMessageDispatcher, tcpRoutingContextContainer, payloadUtils, gatekeeper);
         transport.Initialize();
         mobOperations.RegisterMob(Guid.NewGuid(), new TcpDebugMob(tcpRoutingContextContainer));
         return transport;
      }

      public static TcpTransportFactory CreateServer(int port) {
         var configuration = new TcpTransportConfiguration(
            new IPEndPoint(IPAddress.Any, port),
            TcpRole.Server);
         return new TcpTransportFactory(configuration);
      }

      public static TcpTransportFactory CreateClient(IPAddress address, int port, TcpTransportHandshakeCompletionHandler handshakeCompletionHandler = null) {
         var configuration = new TcpTransportConfiguration(
            new IPEndPoint(address, port),
            TcpRole.Client);
         if (handshakeCompletionHandler != null) {
            configuration.HandshakeCompleted += handshakeCompletionHandler;
         }
         return new TcpTransportFactory(configuration);
      }
   }
}
