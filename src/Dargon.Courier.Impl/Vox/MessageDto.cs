using Dargon.Vox;
using System;

namespace Dargon.Courier.Vox {
   [AutoSerializable]
   public class MessageDto {
      public Guid Id { get; set; }
      public MessageFlags Flags { get; set; }
   }
}
