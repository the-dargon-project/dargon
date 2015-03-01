using System;
using Dargon.Courier.Messaging;
using Dargon.PortableObjects;

namespace Dargon.Courier.PortableObjects {
   public class CourierMessageV1 : IPortableObject {
      private Guid id;
      private Guid recipientId;
      private MessageFlags messageFlags;
      private byte[] payload;

      public CourierMessageV1() { }

      public CourierMessageV1(Guid id, Guid recipientId, MessageFlags messageFlags, byte[] payload) {
         this.id = id;
         this.recipientId = recipientId;
         this.messageFlags = messageFlags;
         this.payload = payload;
      }
      
      public Guid Id { get { return id; } }
      public Guid RecipientId { get { return recipientId; } }
      public MessageFlags MessageFlags { get { return messageFlags; } }
      public byte[] Payload { get { return payload; } }

      public void Serialize(IPofWriter writer) {
         writer.WriteGuid(0, id);
         writer.WriteGuid(1, recipientId);
         writer.WriteU32(2, (uint)messageFlags);
         writer.WriteBytes(3, payload);
      }

      public void Deserialize(IPofReader reader) {
         id = reader.ReadGuid(0);
         recipientId = reader.ReadGuid(1);
         messageFlags = (MessageFlags)reader.ReadU32(2);
         payload = reader.ReadBytes(3);
      }
   }
}
