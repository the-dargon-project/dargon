using System;
using Dargon.Courier.Vox;
using Dargon.Vox2;

namespace Dargon.Courier.TransportTier.Udp.Vox {
   [VoxType((int)CourierVoxTypeIds.AcknowledgementDto)]
   public class AcknowledgementDto {
      public Guid MessageId { get; set; }

      public static AcknowledgementDto Create(Guid messageId) {
         return new AcknowledgementDto {
            MessageId = messageId
         };
      }
   }
}