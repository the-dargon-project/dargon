using Dargon.Courier.AsyncPrimitives;
using Dargon.Courier.PeeringTier;
using Dargon.Courier.RoutingTier;
using Dargon.Courier.TransportTier.Udp.Vox;
using Dargon.Courier.Vox;
using Dargon.Vox;
using System;
using System.Collections.Concurrent;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Dargon.Commons.Pooling;
using Dargon.Vox.Utilities;
using NLog;

namespace Dargon.Courier.TransportTier.Udp {
   public class UdpDispatcher {
      private static readonly Logger logger = LogManager.GetCurrentClassLogger();
      private readonly ConcurrentDictionary<Guid, RoutingContext> routingContextsByPeerId = new Commons.Collections.ConcurrentDictionary<Guid, RoutingContext>();

      private readonly Identity identity;
      private readonly UdpClient udpClient;
      private readonly MessageSender messageSender;
      private readonly DuplicateFilter duplicateFilter;
      private readonly PayloadSender payloadSender;
      private readonly AcknowledgementCoordinator acknowledgementCoordinator;
      private readonly RoutingTable routingTable;
      private readonly PeerTable peerTable;
      private readonly InboundMessageDispatcher inboundMessageDispatcher;

      private volatile bool isShutdown = false;

      public UdpDispatcher(Identity identity, UdpClient udpClient, MessageSender messageSender, DuplicateFilter duplicateFilter, PayloadSender payloadSender, AcknowledgementCoordinator acknowledgementCoordinator, RoutingTable routingTable, PeerTable peerTable, InboundMessageDispatcher inboundMessageDispatcher) {
         this.identity = identity;
         this.udpClient = udpClient;
         this.messageSender = messageSender;
         this.duplicateFilter = duplicateFilter;
         this.payloadSender = payloadSender;
         this.acknowledgementCoordinator = acknowledgementCoordinator;
         this.routingTable = routingTable;
         this.peerTable = peerTable;
         this.inboundMessageDispatcher = inboundMessageDispatcher;
      }

      public async Task InboundSomethingEventHandlerAsync(InboundDataEvent e) {
         await Task.Yield();

         object payload;
         try {
            payload = Deserialize.From(e.Data);
         } catch (Exception ex) {
            if (!isShutdown) {
               logger.Warn("Error at payload deserialize", ex);
            }
            return;
         }
         if (payload is AcknowledgementDto) {
            await HandleAcknowledgementAsync((AcknowledgementDto)payload);
         } else if (payload is AnnouncementDto) {
            await HandleAnnouncementAsync((AnnouncementDto)payload);
         } else if (payload is PacketDto) {
            await HandlePacketDtoAsync((PacketDto)payload);
         }
      }

      private async Task HandleAcknowledgementAsync(AcknowledgementDto x) {
         await acknowledgementCoordinator.ProcessAcknowledgementAsync(x.MessageId);
      }

      private async Task HandleAnnouncementAsync(AnnouncementDto x) {
         var peerIdentity = x.Identity;
         var peerId = peerIdentity.Id;
         bool isNewlyDiscoveredRoute = false;
         var routingContext = routingContextsByPeerId.GetOrAdd(
            peerId,
            add => {
               isNewlyDiscoveredRoute = true;
               return new RoutingContext(messageSender);
            });
         if (isNewlyDiscoveredRoute) {
            routingTable.Register(peerId, routingContext);
         }

         await peerTable.GetOrAdd(peerId).HandleInboundPeerIdentityUpdate(peerIdentity);
      }

      private async Task HandlePacketDtoAsync(PacketDto x) {
         if (!identity.Matches(x.ReceiverId, IdentityMatchingScope.Broadcast)) {
            return;
         }

         if (x.IsReliable()) {
            if (!duplicateFilter.IsNew(x.Id)) {
               return;
            }
            await payloadSender.SendAsync(AcknowledgementDto.Create(x.Id));
         }

         RoutingContext peerRoutingContext;
         if (routingContextsByPeerId.TryGetValue(x.SenderId, out peerRoutingContext)) {
            peerRoutingContext.Weight++;
         }

         await inboundMessageDispatcher.DispatchAsync(x.Message);
      }

      public void Shutdown() {
         isShutdown = true;
         foreach (var kvp in routingContextsByPeerId) {
            routingTable.Unregister(kvp.Key, kvp.Value);
         }
      }

      private class RoutingContext : IRoutingContext {
         private readonly MessageSender messageSender;

         public RoutingContext(MessageSender messageSender) {
            this.messageSender = messageSender;
         }

         public int Weight { get; set; }

         public Task SendBroadcastAsync(MessageDto message) {
            return messageSender.SendBroadcastAsync(message);
         }

         public Task SendReliableAsync(Guid destination, MessageDto message) {
            return messageSender.SendReliableAsync(destination, message);
         }

         public Task SendUnreliableAsync(Guid destination, MessageDto message) {
            return messageSender.SendUnreliableAsync(destination, message);
         }
      }
   }
}
