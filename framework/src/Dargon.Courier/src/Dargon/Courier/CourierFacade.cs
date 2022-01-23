using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dargon.Commons.Collections;
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

namespace Dargon.Courier {
   public class CourierFacade {
      private readonly ConcurrentSet<ITransport> transports;
      private readonly IRyuContainer container;

      public CourierFacade(ConcurrentSet<ITransport> transports, IRyuContainer container) {
         this.transports = transports;
         this.container = container;
      }

      public IReadOnlySet<ITransport> Transports => transports;
      public IRyuContainer Container => container;

      public Identity Identity { get; init; }
      public InboundMessageRouter InboundMessageRouter { get; init; }
      public PeerTable PeerTable { get; init; }
      public RoutingTable RoutingTable { get; init; }
      public Messenger Messenger { get; init; }
      public LocalServiceRegistry LocalServiceRegistry { get; init; }
      public RemoteServiceProxyContainer RemoteServiceProxyContainer { get; init; }
      public MobOperations MobOperations { get; init; }
      public ManagementObjectService ManagementObjectService { get; init; }
      public Publisher Publisher { get; init; }
      public Subscriber Subscriber { get; init; }
      public PubSubClient PubSubClient { get; init; }

      public async Task ShutdownAsync() {
         foreach (var transport in transports) {
            await transport.ShutdownAsync().ConfigureAwait(false);
         }
      }
   }
}
