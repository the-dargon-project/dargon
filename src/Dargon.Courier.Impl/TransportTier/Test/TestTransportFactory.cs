using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dargon.Courier.AuditingTier;
using Dargon.Courier.ManagementTier;
using Dargon.Courier.PeeringTier;
using Dargon.Courier.RoutingTier;
using Dargon.Courier.TransportTier.Udp;
using Dargon.Courier.Vox;
using Dargon.Ryu;

namespace Dargon.Courier.TransportTier.Test {
   public class TestTransportFactory : ITransportFactory {
      private readonly object synchronization = new object();
      private readonly List<TestTransport> transports = new List<TestTransport>();

      public async Task<ITransport> CreateAsync(MobOperations mobOperations, Identity identity, RoutingTable routingTable, PeerTable peerTable, InboundMessageDispatcher inboundMessageDispatcher, AuditService auditService) {
         var transport = new TestTransport(this, identity, routingTable, peerTable, inboundMessageDispatcher);
         transports.Add(transport);

         // transport discovers existing test transports, they discover transport
         foreach (var otherTransport in transports) {
            if (otherTransport == transport) continue;

            var otherTransportNewTransportRoutingContext = new TestRoutingContext(this, identity);
            otherTransport.SetupRoutingContext(otherTransportNewTransportRoutingContext);
            await otherTransport.PeerTable.GetOrAdd(identity.Id).HandleInboundPeerIdentityUpdate(identity);

            var newTransportOtherTransportRoutingContext = new TestRoutingContext(this, otherTransport.Identity);
            transport.SetupRoutingContext(newTransportOtherTransportRoutingContext);
            await transport.PeerTable.GetOrAdd(otherTransport.Identity.Id).HandleInboundPeerIdentityUpdate(otherTransport.Identity);
         }

         return transport;
      }

      public async Task SendMessageBroadcastAsync(MessageDto message) {
         await DispatchOnAllTransports(message);
      }

      public async Task SendMessageReliableAsync(Guid destination, MessageDto message) {
         await DispatchOnAllTransports(message);
      }

      public async Task SendMessageUnreliableAsync(Guid destination, MessageDto message) {
         await DispatchOnAllTransports(message);
      }

      private async Task DispatchOnAllTransports(MessageDto message) {
         foreach (var transport in transports) {
            await transport.InboundMessageDispatcher.DispatchAsync(message);
         }
      }
   }

   public class TestRoutingContext : IRoutingContext {
      private readonly TestTransportFactory testTransportFactory;
      private readonly Identity remoteIdentity;

      public TestRoutingContext(TestTransportFactory testTransportFactory, Identity remoteIdentity) {
         this.testTransportFactory = testTransportFactory;
         this.remoteIdentity = remoteIdentity;
      }

      public Identity RemoteIdentity => remoteIdentity;
      public int Weight => 1;

      public Task SendBroadcastAsync(MessageDto message) {
         return testTransportFactory.SendMessageBroadcastAsync(message);
      }

      public Task SendUnreliableAsync(Guid destination, MessageDto message) {
         return testTransportFactory.SendMessageUnreliableAsync(destination, message);
      }

      public Task SendReliableAsync(Guid destination, MessageDto message) {
         return testTransportFactory.SendMessageReliableAsync(destination, message);
      }
   }
}