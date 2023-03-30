using Dargon.Vox2;
using System;

namespace Dargon.Courier.Vox {
   [VoxType((int)CourierVoxTypeIds.MessageDto)]
   public partial class MessageDto {
      public Guid SenderId { get; set; }
      public Guid ReceiverId { get; set; }
      [P] public object Body { get; set; }

      public static MessageDto Create(Guid senderId, Guid receiverId, object body) => new MessageDto {
         SenderId = senderId,
         ReceiverId = receiverId,
         Body = body
      };

      public override string ToString() => $"[Message SenderId={SenderId}, ReceiverId={ReceiverId}, Body={Body}]";
   }
}
