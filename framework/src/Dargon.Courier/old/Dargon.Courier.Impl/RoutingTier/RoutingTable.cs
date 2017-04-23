using Dargon.Commons;
using Dargon.Commons.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dargon.Courier.RoutingTier {
   public class RoutingTable {
      private readonly CopyOnAddDictionary<Guid, CopyOnModifyRoutingContextContainer> routingContextByPeerId = new CopyOnAddDictionary<Guid, CopyOnModifyRoutingContextContainer>();

      public void Register(Guid peerId, IRoutingContext context) {
         CopyOnModifyRoutingContextContainer routingContextContainer = routingContextByPeerId.GetOrAdd(
            peerId,
            add => new CopyOnModifyRoutingContextContainer());
         routingContextContainer.Add(context);
      }

      public void Unregister(Guid peerId, IRoutingContext context) {
         CopyOnModifyRoutingContextContainer routingContextContainer;
         if (routingContextByPeerId.TryGetValue(peerId, out routingContextContainer)) {
            routingContextContainer.Remove(context);
         }
      }

      public bool TryGetRoutingContext(Guid peerId, out IRoutingContext context) {
         CopyOnModifyRoutingContextContainer routingContextContainer;
         if (routingContextByPeerId.TryGetValue(peerId, out routingContextContainer)) {
            context = routingContextContainer.TakeRandomOrNull();
            return context != null;
         }
         context = null;
         return false;
      }

      public IEnumerable<IRoutingContext> Enumerate() => routingContextByPeerId.Values.SelectMany(routingContexts => routingContexts.Enumerate());

      public class CopyOnModifyRoutingContextContainer {
         private readonly object updateLock = new object();
         private List<IRoutingContext> container = new List<IRoutingContext>();

         public IEnumerable<IRoutingContext> Enumerate() => container;

         public IRoutingContext TakeRandomOrNull() {
            var capture = container;
            if (capture.Any()) {
               return capture.SelectRandomWeighted(c => c.Weight);
            }
            return null;
         }

         public void Add(IRoutingContext routingContext) {
            lock (updateLock) {
               var clone = new List<IRoutingContext>(container);
               if (!clone.Contains(routingContext)) {
                  clone.Add(routingContext);
               }
               container = clone;
            }
         }

         public void Remove(IRoutingContext routingContext) {
            lock (updateLock) {
               var clone = new List<IRoutingContext>(container);
               if (clone.Remove(routingContext)) {
                  container = clone;
               }
            }
         }
      }
   }
}
