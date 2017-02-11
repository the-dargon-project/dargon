using System;
using Dargon.Vox;

namespace Dargon.Courier.TransportTier.Udp.Vox {
   [AutoSerializable]
   public class AcknowledgementDto {
      public Guid MessageId { get; set; }

      public static AcknowledgementDto Create(Guid messageId) {
         return new AcknowledgementDto {
            MessageId = messageId
         };
      }
   }
}