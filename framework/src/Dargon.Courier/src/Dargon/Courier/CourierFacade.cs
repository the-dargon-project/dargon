using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dargon.Commons.Collections;
using Dargon.Courier.ManagementTier;
using Dargon.Courier.PeeringTier;
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

      public IRyuContainer Container { get; set; }
      public Identity Identity => container.GetOrThrow<Identity>();
      public InboundMessageRouter InboundMessageRouter => container.GetOrThrow<InboundMessageRouter>();
      public PeerTable PeerTable => container.GetOrThrow<PeerTable>();
      public RoutingTable RoutingTable => container.GetOrThrow<RoutingTable>();
      public Messenger Messenger => container.GetOrThrow<Messenger>();
      public LocalServiceRegistry LocalServiceRegistry => container.GetOrThrow<LocalServiceRegistry>();
      public RemoteServiceProxyContainer RemoteServiceProxyContainer => container.GetOrThrow<RemoteServiceProxyContainer>();
      public MobOperations MobOperations => container.GetOrThrow<MobOperations>();
      public ManagementObjectService ManagementObjectService => container.GetOrThrow<ManagementObjectService>();

      public async Task ShutdownAsync() {
         foreach (var transport in transports) {
            await transport.ShutdownAsync().ConfigureAwait(false);
         }
      }
   }
}
