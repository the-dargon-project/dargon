using Dargon.Commons.Collections;
using Dargon.Courier.TransportTier.Tcp.Server;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dargon.Commons.Exceptions;

namespace Dargon.Courier.TransportTier.Tcp {
   public class TcpRoutingContextContainer {
      private readonly ConcurrentSet<TcpRoutingContext> routingContexts = new ConcurrentSet<TcpRoutingContext>();
      private readonly ConcurrentDictionary<Guid, TcpRoutingContext> clientRoutingContextsByClientId = new ConcurrentDictionary<Guid, TcpRoutingContext>();

      public IEnumerable<TcpRoutingContext> Enumerate() => routingContexts;

      public void AddOrThrow(TcpRoutingContext routingContext) {
         if (!routingContexts.TryAdd(routingContext)) {
            throw new InvalidStateException();
         }
      }

      public void RemoveOrThrow(TcpRoutingContext routingContext) {
         if (!routingContexts.TryRemove(routingContext)) {
            throw new InvalidStateException();
         }
      }

      public bool TryGetByRemoteId(Guid remoteId, out TcpRoutingContext routingContext) {
         return clientRoutingContextsByClientId.TryGetValue(remoteId, out routingContext);
      }

      public void AssociateRemoteIdentityOrThrow(Guid remoteId, TcpRoutingContext routingContext) {
         clientRoutingContextsByClientId.AddOrThrow(remoteId, routingContext);
      }

      public void UnassociateRemoteIdentityOrThrow(Guid remoteId, TcpRoutingContext routingContext) {
         clientRoutingContextsByClientId.RemoveOrThrow(remoteId, routingContext);
      }

      public async Task ShutdownAsync() {
         foreach (TcpRoutingContext routingContext in routingContexts) {
            await routingContext.ShutdownAsync().ConfigureAwait(false);
         };
      }
   }
}
