using System.Collections.Generic;
using System.Threading.Tasks;
using Dargon.Commons.AsyncAwait;
using Dargon.Commons.Collections;
using Dargon.Courier.AccessControlTier;
using Dargon.Courier.AuditingTier;
using Dargon.Courier.ManagementTier;
using Dargon.Courier.PeeringTier;
using Dargon.Courier.PubSubTier;
using Dargon.Courier.PubSubTier.Publishers;
using Dargon.Courier.PubSubTier.Subscribers;
using Dargon.Courier.RoutingTier;
using Dargon.Courier.ServiceTier.Client;
using Dargon.Courier.ServiceTier.Server;
using Dargon.Courier.TransportTier;
using Dargon.Ryu;
using Dargon.Ryu.Attributes;
using Dargon.Vox2;

namespace Dargon.Courier {
   [RyuDoNotAutoActivate]
   public class CourierFacade {
      private readonly ConcurrentSet<ITransport> transports;
      private readonly IRyuContainer container;

      public CourierFacade(ConcurrentSet<ITransport> transports, IRyuContainer container) {
         this.transports = transports;
         this.container = container;
      }

      public IReadOnlySet<ITransport> Transports => transports;
      public IRyuContainer Container => container;

      public required VoxContext VoxContext { get; init; }
      public required CourierSynchronizationContexts SynchronizationContexts { get; init; }
      public required IGatekeeper Gatekeeper { get; init; }
      public required AuditService AuditService { get; init; }
      
      public required Identity Identity { get; init; }
      public required PeerTable PeerTable { get; init; }
      public required RoutingTable RoutingTable { get; init; }
      
      public required InboundMessageRouter InboundMessageRouter { get; init; }
      public required InboundMessageDispatcher InboundMessageDispatcher { get; init; }
      
      public required Messenger Messenger { get; init; }
      
      public required LocalServiceRegistry LocalServiceRegistry { get; init; }
      public required RemoteServiceProxyContainer RemoteServiceProxyContainer { get; init; }
      
      public required MobOperations MobOperations { get; init; }
      public required ManagementObjectService ManagementObjectService { get; init; }
      
      public required Publisher Publisher { get; init; }
      public required Subscriber Subscriber { get; init; }
      public required PubSubClient PubSubClient { get; init; }

      public async Task<ITransport> AddTransportAsync(ITransportFactory transportFactory) {
         await SynchronizationContexts.CourierDefault__.YieldToAsync();
         var transport = transportFactory.Create(VoxContext, MobOperations, Identity, RoutingTable, PeerTable, InboundMessageDispatcher, AuditService, Gatekeeper);
         transports.TryAdd(transport);
         return transport;
      }

      public async Task ShutdownAsync() {
         foreach (var transport in transports) {
            await transport.ShutdownAsync().ConfigureAwait(false);
         }
      }
   }
}
