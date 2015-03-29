using Dargon.PortableObjects;
using System;

namespace Dargon.Courier.PortableObjects {
   public class CourierMessageAcknowledgeV1 : IPortableObject {
      private Guid id;
      private Guid recipientId;

      public CourierMessageAcknowledgeV1() { }

      public CourierMessageAcknowledgeV1(Guid id, Guid recipientId) {
         this.id = id;
         this.recipientId = recipientId;
      }

      public Guid Id { get { return id; } }
      public Guid RecipientId { get { return recipientId; } }


      public void Serialize(IPofWriter writer) {
         writer.WriteGuid(0, id);
         writer.WriteGuid(1, recipientId);
      }

      public void Deserialize(IPofReader reader) {
         id = reader.ReadGuid(0);
         recipientId = reader.ReadGuid(1);
      }
   }
}
