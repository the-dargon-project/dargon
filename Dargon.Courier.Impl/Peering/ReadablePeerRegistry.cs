using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Courier.PortableObjects;
using Dargon.PortableObjects;
using ItzWarty;
using ItzWarty.Collections;

namespace Dargon.Courier.Peering {
   public interface ReadablePeerRegistry {
   }

   public interface ManageablePeerRegistry : ReadablePeerRegistry {
      void HandlePeerAnnounce(Guid senderId, CourierAnnounceV1 announce);
   }

   public class PeerRegistryImpl : ManageablePeerRegistry {
      private readonly IConcurrentDictionary<Guid, ManageablePeerContext> peerContextsById;
      private readonly IPofSerializer courierSerializer;

      public PeerRegistryImpl(IPofSerializer courierSerializer) : this(courierSerializer, new ConcurrentDictionary<Guid, ManageablePeerContext>()) { }

      public PeerRegistryImpl(IPofSerializer courierSerializer, IConcurrentDictionary<Guid, ManageablePeerContext> peerContextsById) {
         this.courierSerializer = courierSerializer;
         this.peerContextsById = peerContextsById;
      }

      public void HandlePeerAnnounce(Guid senderId, CourierAnnounceV1 announce) {
         peerContextsById.AddOrUpdate(
            senderId, 
            id => new PeerContextImpl(id, new RevisionCounterImpl(), courierSerializer).With(x => x.HandlePeerAnnounce(announce)),
            (id, context) => context.With(x => x.HandlePeerAnnounce(announce)) 
         );
      }

      public IEnumerable<ManageablePeerContext> EnumeratePeers() {
         return peerContextsById.Values;
      }
   }
}
