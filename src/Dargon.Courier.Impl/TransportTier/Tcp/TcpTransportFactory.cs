using Dargon.Courier.PeeringTier;
using Dargon.Courier.RoutingTier;
using System.Net;
using System.Threading.Tasks;

namespace Dargon.Courier.TransportTier.Tcp.Server {
   public class TcpTransportFactory : ITransportFactory {
      private readonly IPEndPoint remoteEndpoint;
      private readonly TcpRole role;

      private TcpTransportFactory(IPEndPoint remoteEndpoint, TcpRole role) {
         this.remoteEndpoint = remoteEndpoint;
         this.role = role;
      }

      public Task<ITransport> CreateAsync(Identity identity, RoutingTable routingTable, PeerTable peerTable, InboundMessageDispatcher inboundMessageDispatcher) {
         var transport = new TcpTransport(remoteEndpoint, role, identity, routingTable, peerTable, inboundMessageDispatcher);
         transport.Initialize();
         return Task.FromResult<ITransport>(transport);
      }

      public static TcpTransportFactory CreateServer(int port) {
         return new TcpTransportFactory(new IPEndPoint(IPAddress.Any, port), TcpRole.Server);
      }

      public static TcpTransportFactory CreateClient(IPAddress address, int port) {
         return new TcpTransportFactory(new IPEndPoint(address, port), TcpRole.Client);
      }
   }
}
