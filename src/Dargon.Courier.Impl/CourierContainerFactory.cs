using System;
using Dargon.Commons.Pooling;
using Dargon.Courier.AsyncPrimitives;
using Dargon.Courier.PacketTier;
using Dargon.Courier.PeeringTier;
using Dargon.Courier.TransitTier;
using Dargon.Courier.Vox;
using Dargon.Ryu;
using Dargon.Vox;
using Fody.Constructors;
using System.IO;

namespace Dargon.Courier {
   [RequiredFieldsConstructor]
   public class CourierContainerFactory {
      private readonly IRyuContainer root = null;

      public IRyuContainer Create() {
         // Transit Tier - UDP
         var outboundDataBus = new AsyncBus<MemoryStream>();
         var inboundDataEventBus = new AsyncBus<InboundDataEvent>();
         var transport = UdpTransport.Create(inboundDataEventBus.Poster(), outboundDataBus.Subscriber());

         // Vox Payload Tier - byte[] <-> Vox Serializable
         var inboundPayloadEventRouter = new InboundPayloadEventRouter();
         var inboundPayloadEventPool = ObjectPool.Create(() => new InboundPayloadEvent());
         inboundDataEventBus.Subscribe(async (s, e) => {
            var payload = Deserialize.From(e.Data);
            var inboundPayloadEvent = inboundPayloadEventPool.TakeObject();
            inboundPayloadEvent.DataEvent = e;
            inboundPayloadEvent.Payload = payload;
            await inboundPayloadEventRouter.TryRouteAsync(inboundPayloadEvent);
            inboundPayloadEventPool.ReturnObject(inboundPayloadEvent);
         });

         var outboundDataBufferPool = ObjectPool.Create(() => new MemoryStream(UdpTransport.kMaximumTransportSize));
         var outboundPayloadEventBus = new AsyncBus<OutboundPayloadEvent>();
         outboundPayloadEventBus.Subscribe(async (bus, e) => {
            var ms = outboundDataBufferPool.TakeObject();
            Serialize.To(ms, e.Payload);
            await outboundDataBus.PostAsync(ms);
            ms.SetLength(0); // zeros internal buffer
            outboundDataBufferPool.ReturnObject(ms);
         });
         var outboundPayloadEventEmitter = new OutboundPayloadEventEmitter(outboundPayloadEventBus.Poster());

         //----------------------------------------------------------------------------------------
         // Packet Tier
         //----------------------------------------------------------------------------------------
         // Acknowledgements, Duplicate Detection
         var acknowledger = new Acknowledger(outboundPayloadEventEmitter);
         var acknowledgementCoordinator = new AcknowledgementCoordinator();
         inboundPayloadEventRouter.RegisterHandler<AcknowledgementDto>(acknowledgementCoordinator.ProcessAcknowledgementAsync);

         // Packet Sending
         var duplicateFilter = new DuplicateFilter();
         var inboundPacketEventRouter = new InboundPacketEventRouter();
         var inboundPacketEventDispatcher = new InboundPacketEventDispatcher(duplicateFilter, acknowledger, inboundPacketEventRouter);
         var inboundPacketEventPool = ObjectPool.Create(() => new InboundPacketEvent());
         inboundPayloadEventRouter.RegisterHandler<PacketDto>(async e => {
            var inboundPacketEvent = inboundPacketEventPool.TakeObject();
            inboundPacketEvent.PayloadEvent = e;
            await inboundPacketEventDispatcher.DispatchAsync(inboundPacketEvent);
            inboundPacketEventPool.ReturnObject(inboundPacketEvent);
         });

         var outboundPacketEventBus = new AsyncBus<OutboundPacketEvent>();
         outboundPacketEventBus.Subscribe(async (bus, e) => {
            await outboundPayloadEventEmitter.EmitAsync(e.Packet, e);
         });
         var outboundPacketEventEmitter = new OutboundPacketEventEmitter(
            outboundPacketEventBus.Poster(),
            acknowledgementCoordinator);

         //----------------------------------------------------------------------------------------
         // Peering Tier - Identity, Discovery, Messaging
         //----------------------------------------------------------------------------------------
         // identity
         var identity = Identity.Create();

         // announcement
         var announcer = new Announcer(identity, outboundPayloadEventEmitter);
         announcer.Initialize();

         // discovery
         var peerDiscoveryEventBus = new AsyncBus<PeerDiscoveryEvent>();
         var peerTable = new PeerTable(() => {
            var router = new AsyncRouter<InboundPayloadEvent, InboundPayloadEvent>(
               x => x.Payload.GetType(), x => x);
            var result = new PeerContext(peerDiscoveryEventBus.Poster(), router);
            result.Initialize();
            return result;
         });
         var peerAnnouncementHandler = new PeerAnnouncementHandler(peerTable);
         inboundPayloadEventRouter.RegisterHandler<AnnouncementDto>(peerAnnouncementHandler.HandleAnnouncementAsync);

         // messaging
         var inboundMessageRouter = new InboundMessageRouter();
         var inboundMessageDispatcher = new InboundMessageDispatcher(peerTable, inboundMessageRouter);
         var nongenericInboundMessageToGenericMessageDispatchInvoker = new NongenericInboundMessageToGenericDispatchInvoker();
         inboundPacketEventRouter.RegisterHandler<MessageDto>(
            e => nongenericInboundMessageToGenericMessageDispatchInvoker.InvokeDispatchAsync(
               e, inboundMessageDispatcher));

         inboundMessageRouter.RegisterHandler<Guid>(x => {
//            x.Sender
         });

         var outboundMessageEventBus = new AsyncBus<OutboundMessageEvent>();
         outboundMessageEventBus.Subscribe(async (bus, e) => {
            await outboundPacketEventEmitter.EmitAsync(
               e.Message,
               e.Reliable,
               e.TagEvent);
         });

         var child = root.CreateChildContainer();
         return child;
      }
   }
}
