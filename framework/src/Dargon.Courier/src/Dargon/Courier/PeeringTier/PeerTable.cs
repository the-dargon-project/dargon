using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Dargon.Ryu;

namespace Dargon.Courier.PeeringTier {
   public class PeerTable {
      private readonly ConcurrentDictionary<Guid, PeerContext> peerContextsById = new ConcurrentDictionary<Guid, PeerContext>();
      private readonly IRyuContainer container;
      private readonly Func<PeerTable, Guid, PeerContext> peerContextFactory;

      public PeerTable(IRyuContainer container, Func<PeerTable, Guid, PeerContext> peerContextFactory) {
         this.container = container;
         this.peerContextFactory = peerContextFactory;
      }

      public IRyuContainer Container => container;

      public PeerContext GetOrAdd(Guid id) {
         if (id == Guid.Empty) {
            throw new InvalidOperationException("Cannot get peer context of broadcast.");
         }

         return peerContextsById.GetOrAdd(
            id, add => peerContextFactory(this, id));
      }

      public IEnumerable<PeerContext> Enumerate() {
         return peerContextsById.Values;
      }
   }
}