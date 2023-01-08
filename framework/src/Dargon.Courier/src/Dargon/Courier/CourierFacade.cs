﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dargon.Commons.Collections;
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
using Dargon.Courier.TransportTier.Tcp;
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

      public required CourierSynchronizationContexts SynchronizationContexts { get; init; }
      public required Identity Identity { get; init; }
      public required InboundMessageRouter InboundMessageRouter { get; init; }
      public required InboundMessageDispatcher InboundMessageDispatcher { get; init; }
      public required PeerTable PeerTable { get; init; }
      public required RoutingTable RoutingTable { get; init; }
      public required Messenger Messenger { get; init; }
      public required LocalServiceRegistry LocalServiceRegistry { get; init; }
      public required RemoteServiceProxyContainer RemoteServiceProxyContainer { get; init; }
      public required AuditService AuditService { get; init; }
      public required MobOperations MobOperations { get; init; }
      public required ManagementObjectService ManagementObjectService { get; init; }
      public required Publisher Publisher { get; init; }
      public required Subscriber Subscriber { get; init; }
      public required PubSubClient PubSubClient { get; init; }

      public async Task<ITransport> AddTransportAsync(ITransportFactory transportFactory) {
         var transport = transportFactory.Create(MobOperations, Identity, RoutingTable, PeerTable, InboundMessageDispatcher, AuditService);
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
