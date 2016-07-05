using System;
using Dargon.Vox;

namespace Dargon.Courier.TransportTier.Udp.Vox {
   [AutoSerializable]
   public class AcknowledgementDto {
      public Guid MessageId { get; set; }
      public Guid SenderId { get; set; }
      public Guid DestinationId { get; set; }

      public static AcknowledgementDto Create(Guid messageId, Guid senderId, Guid destinationId) {
         return new AcknowledgementDto {
            MessageId = messageId,
            SenderId = senderId,
            DestinationId = destinationId
         };
      }
   }
}