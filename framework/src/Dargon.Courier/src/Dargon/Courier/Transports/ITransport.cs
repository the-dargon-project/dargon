using Dargon.Courier.Vox;
using System;
using System.Threading.Tasks;

namespace Dargon.Courier.TransportTier {
   public interface ITransport {
      string Description { get; }

      Task SendMessageBroadcastAsync(MessageDto message);
      Task ShutdownAsync();
   }
}