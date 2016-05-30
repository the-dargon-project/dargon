using Dargon.Courier.Vox;
using System;
using System.Threading.Tasks;

namespace Dargon.Courier.TransportTier {
   public interface ITransport {
      Task SendMessageBroadcastAsync(MessageDto message);
      Task SendMessageReliableAsync(Guid destination, MessageDto message);
      Task SendMessageUnreliableAsync(Guid destination, MessageDto message);

      Task ShutdownAsync();
   }
}