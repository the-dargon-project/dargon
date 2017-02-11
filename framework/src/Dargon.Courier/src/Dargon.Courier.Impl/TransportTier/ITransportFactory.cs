using System.Threading.Tasks;
using Dargon.Courier.AuditingTier;
using Dargon.Courier.ManagementTier;
using Dargon.Courier.PeeringTier;
using Dargon.Courier.RoutingTier;
using Dargon.Ryu;

namespace Dargon.Courier.TransportTier {
   public interface ITransportFactory {
      Task<ITransport> CreateAsync(MobOperations mobOperations, Identity identity, RoutingTable routingTable, PeerTable peerTable, InboundMessageDispatcher inboundMessageDispatcher, AuditService auditService);
   }
}