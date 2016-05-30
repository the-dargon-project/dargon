using System;
using System.Threading.Tasks;
using Dargon.Courier.Vox;

namespace Dargon.Courier.TransportTier.Udp {
   public class UdpTransport : ITransport {
      private readonly MessageSender messageSender;
      private readonly UdpFacade udpFacade;

      public UdpTransport(MessageSender messageSender, UdpFacade udpFacade) {
         this.messageSender = messageSender;
         this.udpFacade = udpFacade;
      }

      public Task SendMessageBroadcastAsync(MessageDto message) {
         return messageSender.SendBroadcastAsync(message);
      }

      public Task SendMessageReliableAsync(Guid destination, MessageDto message) {
         return messageSender.SendReliableAsync(destination, message);
      }

      public Task SendMessageUnreliableAsync(Guid destination, MessageDto message) {
         return messageSender.SendUnreliableAsync(destination, message);
      }

      public Task ShutdownAsync() {
         return udpFacade.ShutdownAsync();
      }
   }
}