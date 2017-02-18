using System;
using System.Linq;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Commons.Collections;
using Dargon.Courier.PeeringTier;
using Dargon.Courier.RoutingTier;
using Dargon.Courier.Vox;

namespace Dargon.Courier.TransportTier.Test {
   public class TestTransport : ITransport {
      private readonly IConcurrentSet<TestRoutingContext> routingContexts = new ConcurrentSet<TestRoutingContext>();
      private readonly TestTransportFactory testTransportFactory;
      private readonly Identity identity;
      private readonly RoutingTable routingTable;
      private readonly PeerTable peerTable;
      private readonly InboundMessageDispatcher inboundMessageDispatcher;

      public TestTransport(TestTransportFactory testTransportFactory, Identity identity, RoutingTable routingTable, PeerTable peerTable, InboundMessageDispatcher inboundMessageDispatcher) {
         this.testTransportFactory = testTransportFactory;
         this.identity = identity;
         this.routingTable = routingTable;
         this.peerTable = peerTable;
         this.inboundMessageDispatcher = inboundMessageDispatcher;
      }

      public Identity Identity => identity;
      public RoutingTable RoutingTable => routingTable;
      public PeerTable PeerTable => peerTable;
      public InboundMessageDispatcher InboundMessageDispatcher => inboundMessageDispatcher;

      public Task SendMessageBroadcastAsync(MessageDto message) {
         return testTransportFactory.SendMessageBroadcastAsync(message);
      }

      public void SetupRoutingContext(TestRoutingContext routingContext) {
         routingContexts.Add(routingContext);
         routingTable.Register(routingContext.RemoteIdentity.Id, routingContext);
      }

      public async Task ShutdownAsync() {
         await TaskEx.YieldToThreadPool();

         foreach (var routingContext in routingContexts) {
            routingTable.Unregister(routingContext.RemoteIdentity.Id, routingContext);
         }
      }
   }
}
