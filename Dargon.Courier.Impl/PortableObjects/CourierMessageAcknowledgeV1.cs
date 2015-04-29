using Dargon.PortableObjects;
using System;

namespace Dargon.Courier.PortableObjects {
   public class CourierMessageAcknowledgeV1 : IPortableObject {
      private Guid recipientId;
      private Guid messageId;

      public CourierMessageAcknowledgeV1() { }

      public CourierMessageAcknowledgeV1(Guid recipientId, Guid messageId) {
         this.recipientId = recipientId;
         this.messageId = messageId;
      }

      public Guid RecipientId { get { return recipientId; } }
      public Guid MessageId { get { return messageId; } }


      public void Serialize(IPofWriter writer) {
         writer.WriteGuid(0, recipientId);
         writer.WriteGuid(1, messageId);
      }

      public void Deserialize(IPofReader reader) {
         recipientId = reader.ReadGuid(0);
         messageId = reader.ReadGuid(1);
      }
   }
}
