using System.Threading.Tasks;
using Dargon.Courier.PeeringTier;
using Dargon.Courier.RoutingTier;

namespace Dargon.Courier.TransportTier {
   public interface ITransportFactory {
      Task<ITransport> CreateAsync(Identity identity, RoutingTable routingTable, PeerTable peerTable, InboundMessageDispatcher inboundMessageDispatcher);
   }
}