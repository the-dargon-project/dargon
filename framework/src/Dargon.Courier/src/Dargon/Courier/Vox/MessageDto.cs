using Dargon.Vox;
using System;

namespace Dargon.Courier.Vox {
   [AutoSerializable]
   public class MessageDto {
      public Guid SenderId { get; set; }
      public Guid ReceiverId { get; set; }
      public object Body { get; set; }

      public static MessageDto Create(Guid senderId, Guid receiverId, object body) => new MessageDto {
         SenderId = senderId,
         ReceiverId = receiverId,
         Body = body
      };

      public override string ToString() => $"[Message SenderId={SenderId}, ReceiverId={ReceiverId}, Body={Body}]";
   }
}
