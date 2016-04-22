using System;
using Dargon.Vox;

namespace Dargon.Courier.Vox {
   [AutoSerializable]
   public class AcknowledgementDto {
      public Guid MessageId { get; set; }
   }
}