using Dargon.Courier.AccessControlTier;
using Dargon.Courier.AuditingTier;
using Dargon.Courier.ManagementTier;
using Dargon.Courier.PeeringTier;
using Dargon.Courier.RoutingTier;
using Dargon.Vox2;

namespace Dargon.Courier.TransportTier {
   public interface ITransportFactory {
      ITransport Create(VoxContext vox, MobOperations mobOperations, Identity identity, RoutingTable routingTable, PeerTable peerTable, InboundMessageDispatcher inboundMessageDispatcher, AuditService auditService, IGatekeeper gatekeeper);
   }
}