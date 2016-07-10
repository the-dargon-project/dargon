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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons.Exceptions;
using static Dargon.Commons.Channels.ChannelsExtensions;

namespace Dargon.Courier.TransportTier.Udp {
   public class UdpDispatcher {
      private static readonly Logger logger = LogManager.GetCurrentClassLogger();
      private readonly ConcurrentDictionary<Guid, RoutingContext> routingContextsByPeerId = new Commons.Collections.ConcurrentDictionary<Guid, RoutingContext>();

      private readonly Identity identity;
      private readonly UdpClient udpClient;
      private readonly DuplicateFilter duplicateFilter;
      private readonly PayloadSender payloadSender;
      private readonly AcknowledgementCoordinator acknowledgementCoordinator;
      private readonly RoutingTable routingTable;
      private readonly PeerTable peerTable;
      private readonly InboundMessageDispatcher inboundMessageDispatcher;
      private readonly MultiPartPacketReassembler multiPartPacketReassembler;
      private readonly AuditCounter announcementsReceivedCounter;
      private readonly AuditCounter resendsCounter;
      private readonly AuditAggregator<int> resendsAggregator;
      private readonly AuditCounter tossedCounter;
      private readonly AuditCounter duplicateReceivesCounter;
      private readonly AuditAggregator<int> multiPartChunksBytesReceivedAggregator;
      private readonly AuditAggregator<double> outboundMessageRateLimitAggregator;
      private readonly AuditAggregator<double> sendQueueDepthAggregator;

      private volatile bool isShutdown = false;

      public UdpDispatcher(Identity identity, UdpClient udpClient, DuplicateFilter duplicateFilter, PayloadSender payloadSender, AcknowledgementCoordinator acknowledgementCoordinator, RoutingTable routingTable, PeerTable peerTable, InboundMessageDispatcher inboundMessageDispatcher, MultiPartPacketReassembler multiPartPacketReassembler, AuditCounter announcementsReceivedCounter, AuditCounter resendsCounter, AuditAggregator<int> resendsAggregator, AuditCounter tossedCounter, AuditCounter duplicateReceivesCounter, AuditAggregator<int> multiPartChunksBytesReceivedAggregator, AuditAggregator<double> outboundMessageRateLimitAggregator, AuditAggregator<double> sendQueueDepthAggregator) {
         this.identity = identity;
         this.udpClient = udpClient;
         this.duplicateFilter = duplicateFilter;
         this.payloadSender = payloadSender;
         this.acknowledgementCoordinator = acknowledgementCoordinator;
         this.routingTable = routingTable;
         this.peerTable = peerTable;
         this.inboundMessageDispatcher = inboundMessageDispatcher;
         this.multiPartPacketReassembler = multiPartPacketReassembler;
         this.announcementsReceivedCounter = announcementsReceivedCounter;
         this.resendsCounter = resendsCounter;
         this.resendsAggregator = resendsAggregator;
         this.tossedCounter = tossedCounter;
         this.duplicateReceivesCounter = duplicateReceivesCounter;
         this.multiPartChunksBytesReceivedAggregator = multiPartChunksBytesReceivedAggregator;
         this.outboundMessageRateLimitAggregator = outboundMessageRateLimitAggregator;
         this.sendQueueDepthAggregator = sendQueueDepthAggregator;
      }

      private readonly ConcurrentQueue<Tuple<InboundDataEvent, Action>> inboundDataEventQueue = new Commons.Collections.ConcurrentQueue<Tuple<InboundDataEvent, Action>>();
      private readonly Semaphore inboundDataEventQueueSemaphore = new Semaphore(0, int.MaxValue);

      public void Initialize() {
//         for (var i = 0; i < Math.Max(4, Environment.ProcessorCount * 10); i++) {
//            new Thread(InboundDataEventProcessorThreadStart) { IsBackground = true }.Start();
//         }
      }

//      public void InboundDataEventProcessorThreadStart() {
//         while (true) {
//            inboundDataEventQueueSemaphore.WaitOne();
//            Tuple<InboundDataEvent, Action> inboundData;
//            if (!inboundDataEventQueue.TryDequeue(out inboundData)) {
//               throw new InvalidStateException();
//            }
//
//            var e = inboundData.Item1;
//            var freeE = inboundData.Item2;
//
//            object payload;
//            try {
//               payload = Deserialize.From(new MemoryStream(e.Data, false));
//            } catch (Exception ex) {
//               if (!isShutdown) {
//                  logger.Warn("Error at payload deserialize", ex);
//               }
//               return;
//            }
//
//            freeE();
//
//            try {
//               if (payload is AcknowledgementDto) {
//                  Interlocked.Increment(ref DebugRuntimeStats.in_ack);
//                  HandleAcknowledgement((AcknowledgementDto)payload);
//                  Interlocked.Increment(ref DebugRuntimeStats.in_ack_done);
//               } else if (payload is AnnouncementDto) {
//                  Interlocked.Increment(ref DebugRuntimeStats.in_ann);
//                  HandleAnnouncement((AnnouncementDto)payload);
//                  //               Interlocked.Increment(ref ann_out);
//               } else if (payload is PacketDto) {
//                  Interlocked.Increment(ref DebugRuntimeStats.in_pac);
//                  Go(async () => {
//                     await HandlePacketDtoAsync((PacketDto)payload).ConfigureAwait(false);
//                     Interlocked.Increment(ref DebugRuntimeStats.in_pac_done);
//                  }).Forget();
//                  //               Interlocked.Increment(ref pac_out);
//               }
//            } catch (Exception ex) {
//               logger.Error("HandleInboundDataAsync threw!", ex);
//            }
//         }
//      }

      public void HandleInboundDataEvent(InboundDataEvent e, Action returnInboundDataEvent) {
         Interlocked.Increment(ref DebugRuntimeStats.in_de);

         List<object> payloads = new List<object>();
         try {
            using (var ms = new MemoryStream(e.Data, e.DataOffset, e.DataLength, false)) {
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

         returnInboundDataEvent();

//         Console.WriteLine("RECIEFVIED " + JsonConvert.SerializeObject(payload));

         foreach (var payload in payloads) {
            try {
               if (payload is AcknowledgementDto) {
                  Interlocked.Increment(ref DebugRuntimeStats.in_ack);
                  acknowledgementCoordinator.ProcessAcknowledgement((AcknowledgementDto)payload);
                  Interlocked.Increment(ref DebugRuntimeStats.in_ack_done);
               } else if (payload is AnnouncementDto) {
                  Interlocked.Increment(ref DebugRuntimeStats.in_ann);
                  HandleAnnouncement(e.RemoteInfo, (AnnouncementDto)payload);
                  //               Interlocked.Increment(ref ann_out);
               } else if (payload is PacketDto) {
                  Interlocked.Increment(ref DebugRuntimeStats.in_pac);
                  HandlePacketDtoAndDispatchAsync(e.RemoteInfo, (PacketDto)payload).Forget();
                  Interlocked.Increment(ref DebugRuntimeStats.in_pac_done);
                  //               Interlocked.Increment(ref pac_out);
               }
            } catch (Exception ex) {
               logger.Error("HandleInboundDataAsync threw!", ex);
            }
         }

         //         inboundDataEventQueue.Enqueue(Tuple.Create(e, returnInboundDataEvent));
         //         inboundDataEventQueueSemaphore.Release();
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

               addedUnicaster = new UdpUnicaster(identity, udpClient, acknowledgementCoordinator, unicastRemoteInfo, resendsCounter, resendsAggregator, outboundMessageRateLimitAggregator, sendQueueDepthAggregator);
               return addedRoutingContext = new RoutingContext( addedUnicaster);
            });
         if (addedRoutingContext == routingContext) {
            addedUnicaster.Initialize();
         }
         if (isNewlyDiscoveredRoute) {
            routingTable.Register(peerId, routingContext);
         }

         peerTable.GetOrAdd(peerId).HandleInboundPeerIdentityUpdate(peerIdentity);
      }

      private Task HandlePacketDtoAndDispatchAsync(UdpClientRemoteInfo remoteInfo, PacketDto x) {
         if (!identity.Matches(x.ReceiverId, IdentityMatchingScope.Broadcast)) {
            tossedCounter.Increment();
            return Task.FromResult(false);
         }

         if (x.IsReliable()) {
            Interlocked.Increment(ref DebugRuntimeStats.in_out_ack);
            var ack = AcknowledgementDto.Create(x.Id, identity.Id, x.SenderId);
            RoutingContext routingContext;
            if (routingContextsByPeerId.TryGetValue(x.SenderId, out routingContext)) {
               routingContext.SendAcknowledgementAsync(x.SenderId, ack).Forget();
            } else {
               payloadSender.BroadcastAsync(ack).Forget();
            }
            Interlocked.Increment(ref DebugRuntimeStats.in_out_ack_done);
         }

         if (logger.IsDebugEnabled && x.Message.Body.GetType().FullName.Contains("Service")) {
            logger.Debug($"Routing packet {x.Id} Reliable: {x.IsReliable()} TBody: {x.Message.Body?.GetType().Name ?? "[null]"} Body: {JsonConvert.SerializeObject(x.Message.Body, Formatting.Indented, new JsonConverter[] { new StringEnumConverter() })}");
         }

         //         RoutingContext peerRoutingContext;
         //         if (routingContextsByPeerId.TryGetValue(x.SenderId, out peerRoutingContext)) {
         //            peerRoutingContext.Weight++;
         //         }

         if (x.Message.Body is MultiPartChunkDto) {
            var chunk = (MultiPartChunkDto)x.Message.Body;
            multiPartChunksBytesReceivedAggregator.Put(chunk.BodyLength);
            multiPartPacketReassembler.HandleInboundMultiPartChunk(chunk);
            return Task.FromResult(false);
         } else {
            return Go(async () => {
               if (x.IsReliable() && !await duplicateFilter.IsNewAsync(x.Id).ConfigureAwait(false)) {
                  duplicateReceivesCounter.Increment();
                  return;
               }
               await inboundMessageDispatcher.DispatchAsync(x.Message).ConfigureAwait(false);
            });
         }
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
