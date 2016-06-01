using Dargon.Courier.AsyncPrimitives;
using Dargon.Courier.PeeringTier;
using Dargon.Courier.RoutingTier;
using Dargon.Courier.TransportTier.Udp.Vox;
using Dargon.Courier.Vox;
using Dargon.Vox;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Commons.Pooling;
using Dargon.Courier.AuditingTier;
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
      private readonly MultiPartPacketReassembler multiPartPacketReassembler;
      private readonly AuditCounter announcementsReceivedCounter;
      private readonly AuditCounter tossedCounter;
      private readonly AuditCounter duplicateReceivesCounter;
      private readonly AuditAggregator<int> multiPartChunksBytesReceivedAggregator;

      private volatile bool isShutdown = false;

      public UdpDispatcher(Identity identity, UdpClient udpClient, MessageSender messageSender, DuplicateFilter duplicateFilter, PayloadSender payloadSender, AcknowledgementCoordinator acknowledgementCoordinator, RoutingTable routingTable, PeerTable peerTable, InboundMessageDispatcher inboundMessageDispatcher, MultiPartPacketReassembler multiPartPacketReassembler, AuditCounter announcementsReceivedCounter, AuditCounter tossedCounter, AuditCounter duplicateReceivesCounter, AuditAggregator<int> multiPartChunksBytesReceivedAggregator) {
         this.identity = identity;
         this.udpClient = udpClient;
         this.messageSender = messageSender;
         this.duplicateFilter = duplicateFilter;
         this.payloadSender = payloadSender;
         this.acknowledgementCoordinator = acknowledgementCoordinator;
         this.routingTable = routingTable;
         this.peerTable = peerTable;
         this.inboundMessageDispatcher = inboundMessageDispatcher;
         this.multiPartPacketReassembler = multiPartPacketReassembler;
         this.announcementsReceivedCounter = announcementsReceivedCounter;
         this.tossedCounter = tossedCounter;
         this.duplicateReceivesCounter = duplicateReceivesCounter;
         this.multiPartChunksBytesReceivedAggregator = multiPartChunksBytesReceivedAggregator;
      }

      public async Task HandleInboundDataEventAsync(InboundDataEvent e) {
         await Task.Yield();

         object payload = null;
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
         announcementsReceivedCounter.Increment();

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
            tossedCounter.Increment();
            return;
         }

         if (x.IsReliable()) {
            if (!duplicateFilter.IsNew(x.Id)) {
               duplicateReceivesCounter.Increment();
               return;
            }
            payloadSender.SendAsync(AcknowledgementDto.Create(x.Id)).Forget();
         }

         RoutingContext peerRoutingContext;
         if (routingContextsByPeerId.TryGetValue(x.SenderId, out peerRoutingContext)) {
            peerRoutingContext.Weight++;
         }

         if (x.Message.Body is MultiPartChunkDto) {
            var chunk = (MultiPartChunkDto)x.Message.Body;
            multiPartChunksBytesReceivedAggregator.Put(chunk.BodyLength);
            multiPartPacketReassembler.HandleInboundMultiPartChunk(chunk);
         } else {
            await inboundMessageDispatcher.DispatchAsync(x.Message).ConfigureAwait(false);
         }
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
