using Dargon.Courier.Vox;
using Dargon.Vox;
using System;

namespace Dargon.Courier.TransportTier.Udp.Vox {
   [AutoSerializable]
   public class PacketDto {
      public Guid Id { get; set; }
      public Guid SenderId { get; set; }
      public Guid ReceiverId { get; set; }
      public PacketFlags Flags { get; set; }
      public MessageDto Message { get; set; }

      public bool IsReliable() => (Flags & PacketFlags.Reliable) != 0;

      public static PacketDto Create(Guid sender, Guid receiver, MessageDto message, bool reliable) => new PacketDto {
         Id = Guid.NewGuid(),
         SenderId = sender,
         ReceiverId = receiver,
         Message = message,
         Flags = reliable ? PacketFlags.Reliable : PacketFlags.None
      };
   }
}
