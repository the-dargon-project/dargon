using Dargon.Courier.TransportTier.Udp.Vox;
using Dargon.Courier.Vox;
using System;
using System.Threading.Tasks;

namespace Dargon.Courier.TransportTier.Udp {
   public class MessageSender {
      private readonly Identity identity;
      private readonly PacketSender packetSender;

      public MessageSender(Identity identity, PacketSender packetSender) {
         this.identity = identity;
         this.packetSender = packetSender;
      }

      public Task SendBroadcastAsync(MessageDto message) {
         return SendHelperAsync(message, false, Guid.Empty);
      }

      public Task SendReliableAsync(Guid destination, MessageDto message) {
         return SendHelperAsync(message, true, destination);
      }

      public Task SendUnreliableAsync(Guid destination, MessageDto message) {
         return SendHelperAsync(message, false, destination);
      }

      private Task SendHelperAsync(MessageDto message, bool reliable, Guid destination) {
         return packetSender.SendAsync(
            new PacketDto {
               Id = Guid.NewGuid(),
               SenderId = identity.Id,
               ReceiverId = destination,
               Message = message,
               Flags = reliable ? PacketFlags.Reliable : PacketFlags.None
            });
      }
   }
}
