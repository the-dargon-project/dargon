using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Dargon.Courier.PortableObjects;
using Dargon.PortableObjects;
using ItzWarty;
using ItzWarty.Collections;

namespace Dargon.Courier.Peering {
   public interface ReadablePeerRegistry {
      RemoteCourierEndpoint GetRemoteCourierEndpointOrNull(Guid identifier);
   }

   public interface ManageablePeerRegistry : ReadablePeerRegistry {
      void HandlePeerAnnounce(Guid senderId, CourierAnnounceV1 announce, IPEndPoint remoteEndpoint);
   }

   public class PeerRegistryImpl : ManageablePeerRegistry {
      private readonly IConcurrentDictionary<Guid, RemoteCourierEndpoint> peerContextsById;
      private readonly IPofSerializer courierSerializer;

      public PeerRegistryImpl(IPofSerializer courierSerializer) : this(courierSerializer, new ConcurrentDictionary<Guid, RemoteCourierEndpoint>()) { }

      public PeerRegistryImpl(IPofSerializer courierSerializer, IConcurrentDictionary<Guid, RemoteCourierEndpoint> peerContextsById) {
         this.courierSerializer = courierSerializer;
         this.peerContextsById = peerContextsById;
      }

      public void HandlePeerAnnounce(Guid senderId, CourierAnnounceV1 announce, IPEndPoint remoteEndPoint) {
         peerContextsById.AddOrUpdate(
            senderId, 
            id => new RemoteCourierEndpointImpl(id, announce.Name, new RevisionCounterImpl(), courierSerializer, remoteEndPoint.Address).With(x => x.HandlePeerAnnounce(announce, remoteEndPoint.Address)),
            (id, context) => context.With(x => x.HandlePeerAnnounce(announce, remoteEndPoint.Address)) 
         );
      }

      public RemoteCourierEndpoint GetRemoteCourierEndpointOrNull(Guid identifier) {
         RemoteCourierEndpoint remoteCourierEndpoint;
         peerContextsById.TryGetValue(identifier, out remoteCourierEndpoint);
         return remoteCourierEndpoint;
      }

      public IEnumerable<RemoteCourierEndpoint> EnumeratePeers() {
         return peerContextsById.Values;
      }
   }
}
