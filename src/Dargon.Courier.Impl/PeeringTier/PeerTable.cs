using System;
using Dargon.Commons.Collections;
using Dargon.Ryu;

namespace Dargon.Courier.PeeringTier {
   public class PeerTable {
      private readonly ConcurrentDictionary<Guid, PeerContext> peerContextsById = new ConcurrentDictionary<Guid, PeerContext>();
      private readonly IRyuContainer container;
      private readonly Func<PeerTable, PeerContext> peerContextFactory;

      public PeerTable(IRyuContainer container, Func<PeerTable, PeerContext> peerContextFactory) {
         this.container = container;
         this.peerContextFactory = peerContextFactory;
      }

      public IRyuContainer Container => container;

      public PeerContext GetOrAdd(Guid id) {
         return peerContextsById.GetOrAdd(
            id, add => peerContextFactory(this));
      }
   }
}