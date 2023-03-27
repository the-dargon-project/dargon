using Dargon.Courier.Vox;
using Dargon.Vox2;
using System;

namespace Dargon.Courier.TransportTier.Udp.Vox {
   [VoxType((int)CourierVoxTypeIds.PacketDto)]
   public class PacketDto {
      public Guid Id { get; set; }
      public Guid SenderId { get; set; }
      public Guid ReceiverId { get; set; }
      public PacketFlags Flags { get; set; }
      public MessageDto Message { get; set; }

      public bool IsReliable() => (Flags & PacketFlags.Reliable) != 0;

      public static PacketDto Create(Guid sender, Guid receiver, MessageDto message, bool reliable) => new PacketDto {
         Id = GuidSource.Next(),
         SenderId = sender,
         ReceiverId = receiver,
         Message = message,
         Flags = reliable ? PacketFlags.Reliable : PacketFlags.None
      };

      public static class GuidSource {
         [ThreadStatic] private static bool initialized;
         [ThreadStatic] private static Guid currentGuid;
         [ThreadStatic] private static int count;

         public static Guid Next() {
            if (!initialized || count == 1024) {
               initialized = true;
               count = 0;
               currentGuid = Guid.NewGuid();
            }
            return AddToGuidSomehow(currentGuid, count++);
         }

         private static unsafe Guid AddToGuidSomehow(Guid guid, int value) {
            var bytes = guid.ToByteArray();

            // sue me.
            fixed (byte* pBytes = bytes) {
               *(int*)pBytes += value;
            }

            return new Guid(bytes);
         }
      }
   }
}
