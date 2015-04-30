using Dargon.PortableObjects;
using System;

namespace Dargon.Courier.PortableObjects {
   public class CourierMessageAcknowledgeV1 : IPortableObject {
      public CourierMessageAcknowledgeV1() { }

      public CourierMessageAcknowledgeV1(Guid recipientId, Guid messageId) {
         Update(recipientId, messageId);
      }

      public Guid RecipientId { get; private set; }
      public Guid MessageId { get; private set; }

      public void Update(Guid recipientId, Guid messageId) {
         this.RecipientId = recipientId;
         this.MessageId = messageId;
      }

      public void Serialize(IPofWriter writer) {
         writer.WriteGuid(0, RecipientId);
         writer.WriteGuid(1, MessageId);
      }

      public void Deserialize(IPofReader reader) {
         RecipientId = reader.ReadGuid(0);
         MessageId = reader.ReadGuid(1);
      }
   }
}
