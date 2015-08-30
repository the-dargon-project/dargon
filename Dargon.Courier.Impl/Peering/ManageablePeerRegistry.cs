using System;
using System.Net;
using Dargon.Courier.PortableObjects;

namespace Dargon.Courier.Peering {
   public interface ManageablePeerRegistry : ReadablePeerRegistry {
      void HandlePeerAnnounce(Guid senderId, CourierAnnounceV1 announce, IPEndPoint remoteEndpoint);
   }
}