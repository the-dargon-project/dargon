using Dargon.Courier.PeeringTier;
using Dargon.Courier.Vox;

namespace Dargon.Courier {
   public class InboundDataEvent {
      public byte[] Data { get; set; }
   }

   public class InboundPayloadEvent {
      public InboundDataEvent DataEvent { get; set; }

      public object Payload { get; set; }
   }

   public class InboundPacketEvent {
      public InboundPayloadEvent PayloadEvent { get; set; }

      public PacketDto Packet => (PacketDto)PayloadEvent.Payload;
   }

   public interface InternalRoutableInboundMessageEvent {
      object Body { get; }
   }

   public class InboundMessageEvent<T> : InternalRoutableInboundMessageEvent {
      public InboundPacketEvent PacketEvent { get; set; }

      public MessageDto Message => (MessageDto) PacketEvent.Packet.Payload;
      public T Body => (T)Message.Body;
      public PeerContext Sender { get; set; }

      object InternalRoutableInboundMessageEvent.Body => Body;
   }

   public class OutboundPayloadEvent {
      public object Payload { get; set; }
      public object TagEvent { get; set; }
   }

   public class OutboundPacketEvent {
      public PacketDto Packet { get; set; }
      public object TagEvent { get; set; }
   }

   public class OutboundMessageEvent {
      public MessageDto Message { get; set; }
      public bool Reliable { get; set; }
      public object TagEvent { get; set; }
   }
}