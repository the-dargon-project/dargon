using Dargon.Vox;
using System;

namespace Dargon.Courier.Vox {
   [AutoSerializable]
   public class MessageDto {
      public Guid SenderId { get; set; }
      public Guid ReceiverId { get; set; }
      public object Body { get; set; }
   }
}
