using System;
using Dargon.Vox;

namespace Dargon.Courier.Vox {
   [AutoSerializable]
   public class PacketDto {
      public Guid Id { get; set; }
      public object Payload { get; set; }
      public PacketFlags Flags { get; set;}

      public static PacketDto Create(object payload, PacketFlags flags) {
         return new PacketDto {
            Id = Guid.NewGuid(),
            Payload = payload,
            Flags = flags
         };
      }

      public bool IsReliable() => (Flags & PacketFlags.Reliable) != 0;}
}
