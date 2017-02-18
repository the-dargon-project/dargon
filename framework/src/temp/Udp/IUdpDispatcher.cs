using Dargon.Commons;
using Dargon.Courier.AuditingTier;
using Dargon.Courier.PeeringTier;
using Dargon.Courier.RoutingTier;
using Dargon.Courier.TransportTier.Udp.Vox;
using Dargon.Courier.Vox;
using Dargon.Vox;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NLog;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons.Collections;
using SCG = System.Collections.Generic;
using static Dargon.Commons.Channels.ChannelsExtensions;

namespace Dargon.Courier.TransportTier.Udp {
   public interface IUdpDispatcher {
      void HandleInboundDataEvent(InboundDataEvent e, Action<InboundDataEvent> returnInboundDataEvent);
   }

   public class UdpDispatcherImpl : IUdpDispatcher {
      private static readonly Logger logger = LogManager.GetCurrentClassLogger();
      private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, RoutingContext> routingContextsByPeerId = new Commons.Collections.ConcurrentDictionary<Guid, RoutingContext>();

      private readonly Identity identity;
      private readonly UdpClient udpClient;
      private readonly DuplicateFilter duplicateFilter;
      private readonly PayloadSender payloadSender;
      private readonly AcknowledgementCoordinator acknowledgementCoordinator;
      private readonly RoutingTable routingTable;
      private readonly PeerTable peerTable;
      private readonly InboundMessageDispatcher inboundMessageDispatcher;
      private readonly MultiPartPacketReassembler multiPartPacketReassembler;
      private readonly IUdpUnicasterFactory udpUnicasterFactory;
      private readonly IAuditCounter announcementsReceivedCounter;
      private readonly IAuditCounter tossedCounter;
      private readonly IAuditCounter duplicateReceivesCounter;
      private readonly IAuditAggregator<int> multiPartChunksBytesReceivedAggregator;

      private volatile bool isShutdown = false;

      public UdpDispatcherImpl(Identity identity, UdpClient udpClient, DuplicateFilter duplicateFilter, PayloadSender payloadSender, AcknowledgementCoordinator acknowledgementCoordinator, RoutingTable routingTable, PeerTable peerTable, InboundMessageDispatcher inboundMessageDispatcher, MultiPartPacketReassembler multiPartPacketReassembler, IUdpUnicasterFactory udpUnicasterFactory, IAuditCounter announcementsReceivedCounter, IAuditCounter tossedCounter, IAuditCounter duplicateReceivesCounter, IAuditAggregator<int> multiPartChunksBytesReceivedAggregator) {
         this.identity = identity;
         this.udpClient = udpClient;
         this.duplicateFilter = duplicateFilter;
         this.payloadSender = payloadSender;
         this.acknowledgementCoordinator = acknowledgementCoordinator;
         this.routingTable = routingTable;
         this.peerTable = peerTable;
         this.inboundMessageDispatcher = inboundMessageDispatcher;
         this.multiPartPacketReassembler = multiPartPacketReassembler;
         this.udpUnicasterFactory = udpUnicasterFactory;
         this.announcementsReceivedCounter = announcementsReceivedCounter;
         this.tossedCounter = tossedCounter;
         this.duplicateReceivesCounter = duplicateReceivesCounter;
         this.multiPartChunksBytesReceivedAggregator = multiPartChunksBytesReceivedAggregator;
      }

      /// <summary>
      /// Processes an inbound data event. 
      /// This is assumed to be invoked on an IOCP thread so a goal is to do as little as possible.
      /// </summary>
      public void HandleInboundDataEvent(InboundDataEvent e, Action<InboundDataEvent> returnInboundDataEvent) {
#if DEBUG
         Interlocked.Increment(ref DebugRuntimeStats.in_de);
#endif

         // Deserialize inbound payloads
         SCG.List<object> payloads = new SCG.List<object>();
         try {
            using (var ms = new MemoryStream(e.Data, e.DataOffset, e.DataLength, false, true)) {
               while (ms.Position < ms.Length) {
                  payloads.Add(Deserialize.From(ms));
               }
            }
         } catch (Exception ex) {
            if (!isShutdown) {
               logger.Warn("Error at payload deserialize", ex);
            }
            return;
         }
         returnInboundDataEvent(e);
#if DEBUG
         Interlocked.Add(ref DebugRuntimeStats.in_payload, payloads.Count);
#endif

         // Categorize inbound payloads
         var acknowledgements = new SCG.List<AcknowledgementDto>();
         var announcements = new SCG.List<AnnouncementDto>();
         var reliablePackets = new SCG.List<PacketDto>();
         var unreliablePackets = new SCG.List<PacketDto>();
         foreach (var payload in payloads) {
            if (payload is AcknowledgementDto) {
               acknowledgements.Add((AcknowledgementDto)payload);
            } else if (payload is AnnouncementDto) {
               announcements.Add((AnnouncementDto)payload);
            } else if (payload is PacketDto) {
               // Filter packets not destined to us.
               var packet = (PacketDto)payload;
               if (!identity.Matches(packet.ReceiverId, IdentityMatchingScope.Broadcast)) {
                  tossedCounter.Increment();
                  continue;
               }

               // Bin into reliable vs unreliable.
               if (packet.IsReliable()) {
                  reliablePackets.Add(packet);
               } else {
                  unreliablePackets.Add(packet);
               }
            }
         }

         // Process acks to prevent resends.
         foreach (var ack in acknowledgements) {
#if DEBUG
            Interlocked.Increment(ref DebugRuntimeStats.in_ack);
#endif
            acknowledgementCoordinator.ProcessAcknowledgement(ack);
#if DEBUG
            Interlocked.Increment(ref DebugRuntimeStats.in_ack_done);
#endif
         }

         // Process announcements as they are necessary for routing.
         foreach (var announcement in announcements) {
#if DEBUG
            Interlocked.Increment(ref DebugRuntimeStats.in_ann);
#endif
            HandleAnnouncement(e.RemoteInfo, announcement);
         }

         // Ack inbound reliable messages to prevent resends.
         foreach (var packet in reliablePackets) {
#if DEBUG
            Interlocked.Increment(ref DebugRuntimeStats.in_out_ack);
#endif
            var ack = AcknowledgementDto.Create(packet.Id);
            RoutingContext routingContext;
            if (routingContextsByPeerId.TryGetValue(packet.SenderId, out routingContext)) {
               routingContext.SendAcknowledgementAsync(packet.SenderId, ack).Forget();
            } else {
               payloadSender.BroadcastAsync(ack).Forget();
            }
#if DEBUG
            Interlocked.Increment(ref DebugRuntimeStats.in_out_ack_done);
#endif
         }

         // Test reliable packets' guids against bloom filter.
         var isNewByPacketId = duplicateFilter.TestPacketIdsAreNew(new HashSet<Guid>(reliablePackets.Select(p => p.Id)));
         var standalonePacketsToProcess = new SCG.List<PacketDto>(unreliablePackets);
         var chunksToProcess = new SCG.List<MultiPartChunkDto>();
         foreach (var packet in reliablePackets) {
            // Toss out duplicate packets
            if (!isNewByPacketId[packet.Id]) {
               duplicateReceivesCounter.Increment();
               continue;
            } 

            // Bin into multipart chunk vs not
            var multiPartChunk = packet.Message.Body as MultiPartChunkDto;
            if (multiPartChunk != null) {
               multiPartChunksBytesReceivedAggregator.Put(multiPartChunk.BodyLength);
               chunksToProcess.Add(multiPartChunk);
            } else {
               standalonePacketsToProcess.Add(packet);
            }
         }

         // Kick off async stanadalone packet process on thread pool.
         foreach (var packet in standalonePacketsToProcess) {
            inboundMessageDispatcher.DispatchAsync(packet.Message).Forget();
         }

         // Synchronously handle multipart chunk processing.
         foreach (var chunk in chunksToProcess) {
            multiPartPacketReassembler.HandleInboundMultiPartChunk(chunk);
         }
      }

      private void HandleAnnouncement(UdpClientRemoteInfo remoteInfo, AnnouncementDto x) {
         announcementsReceivedCounter.Increment();

         var peerIdentity = x.Identity;
         var peerId = peerIdentity.Id;
         bool isNewlyDiscoveredRoute = false;
         RoutingContext addedRoutingContext = null;
         UdpUnicaster addedUnicaster = null;
         var routingContext = routingContextsByPeerId.GetOrAdd(
            peerId,
            add => {
               isNewlyDiscoveredRoute = true;
               var unicastReceivePort = int.Parse((string)x.Identity.Properties[UdpConstants.kUnicastPortIdentityPropertyKey]);
               var unicastIpAddress = remoteInfo.IPEndpoint.Address;
               var unicastEndpoint = new IPEndPoint(unicastIpAddress, unicastReceivePort);
               var unicastRemoteInfo = new UdpClientRemoteInfo {
                  Socket = remoteInfo.Socket,
                  IPEndpoint = unicastEndpoint
               };

               addedUnicaster = udpUnicasterFactory.Create(unicastRemoteInfo);
               return addedRoutingContext = new RoutingContext(addedUnicaster);
            });
         if (addedRoutingContext == routingContext) {
            addedUnicaster.Initialize();
         }
         if (isNewlyDiscoveredRoute) {
            routingTable.Register(peerId, routingContext);
         }

         peerTable.GetOrAdd(peerId).HandleInboundPeerIdentityUpdate(peerIdentity);
      }

      public void Shutdown() {
         isShutdown = true;
         foreach (var kvp in routingContextsByPeerId) {
            routingTable.Unregister(kvp.Key, kvp.Value);
         }
      }

      private class RoutingContext : IRoutingContext {
         private readonly UdpUnicaster unicaster;

         public RoutingContext(UdpUnicaster unicaster) {
            this.unicaster = unicaster;
         }

         public int Weight { get; set; }

         public Task SendAcknowledgementAsync(Guid destination, AcknowledgementDto acknowledgement) {
            return unicaster.SendAcknowledgementAsync(destination, acknowledgement);
         }

         public Task SendReliableAsync(Guid destination, MessageDto message) {
            return unicaster.SendReliableAsync(destination, message);
         }

         public Task SendUnreliableAsync(Guid destination, MessageDto message) {
            return unicaster.SendUnreliableAsync(destination, message);
         }
      }
   }
}
