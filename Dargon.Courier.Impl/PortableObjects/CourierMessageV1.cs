using Dargon.Courier.Messaging;
using Dargon.PortableObjects;
using System;
using System.Threading.Tasks;

namespace Dargon.Courier.PortableObjects {
   public class CourierMessageV1 : IPortableObject {
      private Guid id;
      private Guid recipientId;
      private MessageFlags messageFlags;
      private byte[] payload;
      private int payloadOffset;
      private int payloadLength;

      public CourierMessageV1() { }

      public CourierMessageV1(Guid id, Guid recipientId, MessageFlags messageFlags, byte[] payload, int payloadOffset, int payloadLength) {
         this.id = id;
         this.recipientId = recipientId;
         this.messageFlags = messageFlags;
         this.payload = payload;
         this.payloadOffset = payloadOffset;
         this.payloadLength = payloadLength;
      }
      
      public Guid Id { get { return id; } }
      public Guid RecipientId { get { return recipientId; } }
      public MessageFlags MessageFlags { get { return messageFlags; } }
      public byte[] Payload { get { return payload; } }
      public int PayloadOffset {  get { return payloadOffset; } }
      public int PayloadLength { get { return payloadLength; } }

      public void Serialize(IPofWriter writer) {
         writer.WriteGuid(0, id);
         writer.WriteGuid(1, recipientId);
         writer.WriteU32(2, (uint)messageFlags);
         writer.AssignSlot(3, payload, payloadOffset, payloadLength);
      }

      public void Deserialize(IPofReader reader) {
         id = reader.ReadGuid(0);
         recipientId = reader.ReadGuid(1);
         messageFlags = (MessageFlags)reader.ReadU32(2);
         payload = reader.ReadBytes(3);
         payloadOffset = 0;
         payloadLength = payload.Length;
      }
   }
}
