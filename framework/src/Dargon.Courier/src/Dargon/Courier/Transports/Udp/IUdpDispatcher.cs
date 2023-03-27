using Dargon.Commons;
using Dargon.Courier.AuditingTier;
using Dargon.Courier.PeeringTier;
using Dargon.Courier.RoutingTier;
using Dargon.Courier.TransportTier.Udp.Vox;
using Dargon.Courier.Vox;
using Dargon.Vox;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons.AsyncAwait;
using Dargon.Commons.Pooling;
using Dargon.Courier.AccessControlTier;
using Dargon.Courier.Transports.Udp.Raw;

namespace Dargon.Courier.TransportTier.Udp {
   public interface IUdpDispatcher : ICoreUdpReceiveListener { }

   public class UdpDispatcher : IUdpDispatcher {
      private static readonly Logger logger = LogManager.GetCurrentClassLogger();
      private readonly ConcurrentDictionary<Guid, RoutingContext> routingContextsByPeerId = new ConcurrentDictionary<Guid, RoutingContext>();

      private readonly Identity identity;
      private readonly DuplicateFilter duplicateFilter;
      private readonly CoreBroadcaster coreBroadcaster;
      private readonly AcknowledgementCompletionLatchContainer acknowledgementCompletionLatchContainer;
      private readonly RoutingTable routingTable;
      private readonly PeerTable peerTable;
      private readonly InboundMessageDispatcher inboundMessageDispatcher;
      private readonly MultiPartPacketReassembler multiPartPacketReassembler;
      private readonly IAuditCounter announcementsReceivedCounter;
      private readonly IAuditCounter tossedCounter;
      private readonly IAuditCounter duplicateReceivesCounter;
      private readonly IAuditAggregator<int> multiPartChunksBytesReceivedAggregator;
      private readonly IGatekeeper gatekeeper;
      private readonly CourierSynchronizationContexts courierSynchronizationContexts;

      private Func<UdpRemoteInfo, UdpUnicaster> createUdpUnicasterFunc;
      private volatile bool isShutdown = false;

      public UdpDispatcher(Identity identity, DuplicateFilter duplicateFilter, CoreBroadcaster coreBroadcaster, AcknowledgementCompletionLatchContainer acknowledgementCompletionLatchContainer, RoutingTable routingTable, PeerTable peerTable, InboundMessageDispatcher inboundMessageDispatcher, MultiPartPacketReassembler multiPartPacketReassembler, IAuditCounter announcementsReceivedCounter, IAuditCounter tossedCounter, IAuditCounter duplicateReceivesCounter, IAuditAggregator<int> multiPartChunksBytesReceivedAggregator, IGatekeeper gatekeeper, CourierSynchronizationContexts courierSynchronizationContexts) {
         this.identity = identity;
         this.duplicateFilter = duplicateFilter;
         this.coreBroadcaster = coreBroadcaster;
         this.acknowledgementCompletionLatchContainer = acknowledgementCompletionLatchContainer;
         this.routingTable = routingTable;
         this.peerTable = peerTable;
         this.inboundMessageDispatcher = inboundMessageDispatcher;
         this.multiPartPacketReassembler = multiPartPacketReassembler;
         this.announcementsReceivedCounter = announcementsReceivedCounter;
         this.tossedCounter = tossedCounter;
         this.duplicateReceivesCounter = duplicateReceivesCounter;
         this.multiPartChunksBytesReceivedAggregator = multiPartChunksBytesReceivedAggregator;
         this.gatekeeper = gatekeeper;
         this.courierSynchronizationContexts = courierSynchronizationContexts;
      }

      public void SetCreateUdpUnicasterFunc(Func<UdpRemoteInfo, UdpUnicaster> func) {
         createUdpUnicasterFunc = func;
      }

      /// <summary>
      /// Processes an inbound data event. 
      /// This is assumed to be invoked on an IOCP thread so a goal is to do as little as possible.
      /// </summary>
      /// <param name="leasedBufferView">reference must be decremented by callee</param>
      public void HandleInboundUdpPacket(IOpaqueUdpNetworkAdapter adapter, LeasedBufferView leasedBufferView, UdpRemoteInfo remoteInfo) {
         courierSynchronizationContexts.EarlyNetworkIO.AssertIsActivated();

#if DEBUG
         Interlocked.Increment(ref DebugRuntimeStats.in_de);
#endif

         var rawPacketData = leasedBufferView.Span;
         var header = RawUdpPacketHeader.Read(rawPacketData);
         var frameListOffset = RawUdpPacketHeader.kFrameListStartOffset;
         var footerOffset = RawUdpPacketFooter.GetOffset(rawPacketData);
         var footer = RawUdpPacketFooter.Read(rawPacketData);

         // Verify checksum. Note SequenceEqual is an optimized Span method, not LINQ slowness.
         var checksum = SHA256.HashData(rawPacketData.Slice(0, footerOffset));
         if (!footer.Checksum.SequenceEqual(checksum)) {
            throw new Exception("!!!");
         }

         HandleInboundUdpPacketInternalAsync(adapter, leasedBufferView.Transfer, remoteInfo);
      }

      private void HandleInboundUdpPacketInternalAsync(IOpaqueUdpNetworkAdapter adapter, LeasedBufferView leasedBufferView, UdpRemoteInfo remoteInfo) {
         //--------------------------------------------------------------------
         // Deserialize inbound payloads, then free the LBV
         //--------------------------------------------------------------------
         List<object> payloads = new List<object>();
         try {
            using var dataStream = leasedBufferView.CreateStream();

            while (dataStream.Position < dataStream.Length) {
               payloads.Add(VoxDeserialize.From(dataStream));
            }
         } catch (Exception ex) {
            if (!isShutdown) {
               logger.Warn("Error at payload deserialize", ex);
            }

            return;
         } finally {
            leasedBufferView.Release();
            leasedBufferView = null;
         }

#if DEBUG
         Interlocked.Add(ref DebugRuntimeStats.in_payload, payloads.Count);
#endif

         //--------------------------------------------------------------------
         // Categorize inbound payloads
         //--------------------------------------------------------------------
         var acknowledgements = new List<AcknowledgementDto>();
         var announcements = new List<AnnouncementDto>();
         var reliablePackets = new List<PacketDto>();
         var unreliablePackets = new List<PacketDto>();
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
         //--------------------------------------------------------------------
         // Dispatch
         //--------------------------------------------------------------------
         acknowledgementCompletionLatchContainer.ProcessAcknowledgements(acknowledgements);

         // Process announcements as they are necessary for routing.
         foreach (var announcement in announcements) {
#if DEBUG
            Interlocked.Increment(ref DebugRuntimeStats.in_ann);
#endif
            HandleAnnouncement(remoteInfo, announcement);
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
               coreBroadcaster.BroadcastAsync(ack).Forget();
            }
#if DEBUG
            Interlocked.Increment(ref DebugRuntimeStats.in_out_ack_done);
#endif
         }

         // Test reliable packets' guids against bloom filter.
         var isNewByPacketId = duplicateFilter.TestPacketIdsAreNew(new HashSet<Guid>(reliablePackets.Select(p => p.Id)).AsReadOnlySet());
         var standalonePacketsToProcess = new List<PacketDto>(unreliablePackets);
         var chunksToProcess = new List<MultiPartChunkDto>();
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
            // the dispatch itself can throw.
            inboundMessageDispatcher.DispatchAsync(packet.Message).Forget();
         }

         // Synchronously handle multipart chunk processing.
         foreach (var chunk in chunksToProcess) {
            multiPartPacketReassembler.HandleInboundMultiPartChunkAsync(adapter, chunk, remoteInfo).Forget();
         }
      }

      private void HandleAnnouncement(UdpRemoteInfo remoteInfo, AnnouncementDto x) {
         announcementsReceivedCounter.Increment();

         try {
            gatekeeper.ValidateWhoAmI(x.WhoAmI);
         } catch (Exception) {
            return;
         }

         var peerIdentity = x.WhoAmI.Identity;
         var peerId = peerIdentity.Id;
         bool isNewlyDiscoveredRoute = false;
         RoutingContext addedRoutingContext = null;
         UdpUnicaster addedUnicaster = null;
         var routingContext = routingContextsByPeerId.GetOrAdd(
            peerId,
            add => {
               isNewlyDiscoveredRoute = true;
               var unicastReceivePort = int.Parse((string)peerIdentity.DeclaredProperties[UdpConstants.kUnicastPortIdentityPropertyKey]);
               var unicastIpAddress = remoteInfo.IPEndpoint.Address;
               var unicastEndpoint = new IPEndPoint(unicastIpAddress, unicastReceivePort);
               var unicastRemoteInfo = remoteInfo with { IPEndpoint = unicastEndpoint };

               addedUnicaster = createUdpUnicasterFunc(unicastRemoteInfo);
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
