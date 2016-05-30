using Dargon.Courier.Vox;
using System;
using System.Threading.Tasks;

namespace Dargon.Courier.RoutingTier {
   public interface IRoutingContext {
      int Weight { get; }

      Task SendBroadcastAsync(MessageDto message);
      Task SendUnreliableAsync(Guid destination, MessageDto message);
      Task SendReliableAsync(Guid destination, MessageDto message);
   }
}