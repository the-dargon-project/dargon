using System.Net;
using System.Threading;
using Dargon.Courier.Identities;
using Dargon.Courier.PortableObjects;

namespace Dargon.Courier.Peering {
   public interface RemoteCourierEndpoint : ReadableCourierEndpoint {
      void HandlePeerAnnounce(CourierAnnounceV1 announce, IPAddress remoteAddress);
   }
}