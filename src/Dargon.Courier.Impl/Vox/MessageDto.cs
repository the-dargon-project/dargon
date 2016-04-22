using Dargon.Vox;
using System;

namespace Dargon.Courier.Vox {
   [AutoSerializable]
   public class MessageDto {
      public Guid SenderId { get; set; }
      public object Body { get; set; }

      public static MessageDto Create(object payload) {
         return new MessageDto {
            Body = payload,
         };
      }
   }
}
