using Dargon.Courier.Identities;
using Dargon.Courier.Messaging;
using Dargon.Courier.Peering;

namespace Dargon.Courier {
   public interface CourierClient : ManageableCourierEndpoint, MessageSender, MessageRouter, ReadablePeerRegistry {
      ManageableCourierEndpoint LocalEndpoint { get; }
      MessageSender MessageSender { get; }
      MessageRouter MessageRouter { get; }
      ReadablePeerRegistry PeerRegistry { get; }
   }
}
