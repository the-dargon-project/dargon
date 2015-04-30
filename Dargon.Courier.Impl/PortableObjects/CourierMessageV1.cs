using Dargon.Courier.Messaging;
using Dargon.PortableObjects;
using System;
using System.Threading.Tasks;

namespace Dargon.Courier.PortableObjects {
   public class CourierMessageV1 : IPortableObject {
      private const int kUseActualPayloadLength = Int32.MinValue / 2;

      public CourierMessageV1() { }

      public CourierMessageV1(Guid id, Guid recipientId, MessageFlags messageFlags, byte[] payload, int payloadOffset, int payloadLength) {
         Update(id, recipientId, messageFlags, payload, payloadOffset, payloadLength);
      }
      
      public Guid Id { get; set; }
      public Guid RecipientId { get; set; }
      public MessageFlags MessageFlags { get; set; }
      public byte[] Payload { get; set; }
      public int PayloadOffset { get; set; }
      public int PayloadLength { get; set; }

      public void Update(Guid id, Guid recipientId, MessageFlags messageFlags, byte[] payload, int payloadOffset, int payloadLength) {
         this.Id = id;
         this.RecipientId = recipientId;
         this.MessageFlags = messageFlags;
         this.Payload = payload;
         this.PayloadOffset = payloadOffset;
         this.PayloadLength = payloadLength == kUseActualPayloadLength ? payload.Length : payloadLength;
      }


      public void Serialize(IPofWriter writer) {
         writer.WriteGuid(0, Id);
         writer.WriteGuid(1, RecipientId);
         writer.WriteU32(2, (uint)MessageFlags);
         writer.AssignSlot(3, Payload, PayloadOffset, PayloadLength);
      }

      public void Deserialize(IPofReader reader) {
         Update(
            id:  reader.ReadGuid(0),
            recipientId: reader.ReadGuid(1),
            messageFlags: (MessageFlags)reader.ReadU32(2),
            payload: reader.ReadBytes(3),
            payloadOffset: 0,
            payloadLength: kUseActualPayloadLength
         );
      }
   }
}
