using System;
using Dargon.Commons.Collections;

namespace Dargon.Courier.PeeringTier {
   public class PeerTable {
      private readonly ConcurrentDictionary<Guid, PeerContext> peerContextsById = new ConcurrentDictionary<Guid, PeerContext>();
      private readonly Func<PeerContext> peerContextFactory;

      public PeerTable(Func<PeerContext> peerContextFactory) {
         this.peerContextFactory = peerContextFactory;
      }

      public PeerContext GetOrAdd(Guid id) {
         return peerContextsById.GetOrAdd(
            id, add => peerContextFactory());
      }
   }
}