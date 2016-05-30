﻿using System.Net;
using System.Threading.Tasks;
using Dargon.Courier.PeeringTier;
using Dargon.Courier.RoutingTier;
using Dargon.Courier.TransportTier.Tcp.Server;

namespace Dargon.Courier.TransportTier.Tcp {
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

      public event TcpTransportHandshakeCompletionHandler HandshakeCompleted;

      internal void HandleRemoteHandshakeCompletion(Identity remoteIdentity) {
         var e = new TcpTransportHandshakeCompletionEventArgs(remoteIdentity);
         HandshakeCompleted?.Invoke(e);
      }
   }

   public class TcpTransportFactory : ITransportFactory {
      private TcpTransportConfiguration configuration;

      public TcpTransportFactory(TcpTransportConfiguration configuration) {
         this.configuration = configuration;
      }

      public Task<ITransport> CreateAsync(Identity identity, RoutingTable routingTable, PeerTable peerTable, InboundMessageDispatcher inboundMessageDispatcher) {
         var transport = new TcpTransport(configuration, identity, routingTable, peerTable, inboundMessageDispatcher);
         transport.Initialize();
         return Task.FromResult<ITransport>(transport);
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
