using Dargon.Commons;
using Dargon.Commons.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dargon.Courier.RoutingTier {
   public class RoutingTable {
      private readonly ConcurrentDictionary<Guid, ConcurrentSet<IRoutingContext>> routingContextByPeerId = new ConcurrentDictionary<Guid, ConcurrentSet<IRoutingContext>>();

      public void Register(Guid peerId, IRoutingContext context) {
         ConcurrentSet<IRoutingContext> contexts = routingContextByPeerId.GetOrAdd(
            peerId, 
            add => new ConcurrentSet<IRoutingContext>());
         contexts.TryAdd(context);
      }

      public void Unregister(Guid peerId, IRoutingContext context) {
         ConcurrentSet<IRoutingContext> contexts;
         if (routingContextByPeerId.TryGetValue(peerId, out contexts)) {
            contexts.TryRemove(context);
         }
      }

      public bool TryGetRoutingContext(Guid peerId, out IRoutingContext context) {
         ConcurrentSet<IRoutingContext> contextsSet;
         if (routingContextByPeerId.TryGetValue(peerId, out contextsSet)) {
            var contexts = contextsSet.ToList();
            if (contexts.Any()) {
               context = contexts.SelectRandomWeighted(c => c.Weight);
               return true;
            }
         }
         context = null;
         return false;
      }

      public IEnumerable<IRoutingContext> Enumerate() => routingContextByPeerId.Values.SelectMany(routingContexts => routingContexts);
   }
}
