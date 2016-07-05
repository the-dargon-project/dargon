using Dargon.Commons.Collections;
using Dargon.Courier.RoutingTier;
using Dargon.Courier.TransportTier;
using Dargon.Courier.Vox;
using System;
using System.Linq;
using System.Threading.Tasks;
using Dargon.Commons.Exceptions;

namespace Dargon.Courier {
   public class Messenger {
      private readonly Identity identity;
      private readonly IConcurrentSet<ITransport> transports;
      private readonly RoutingTable routingTable;

      public Messenger(Identity identity, IConcurrentSet<ITransport> transports, RoutingTable routingTable) {
         this.identity = identity;
         this.transports = transports;
         this.routingTable = routingTable;
      }

      public Task BroadcastAsync<T>(T payload) {
         var message = new MessageDto {
            Body = payload,
            ReceiverId = Guid.Empty,
            SenderId = identity.Id
         };
         return Task.WhenAll(transports.Select(t => t.SendMessageBroadcastAsync(message)));
      }

      public async Task SendUnreliableAsync<T>(T payload, Guid destination) {
         IRoutingContext routingContext;
         if (routingTable.TryGetRoutingContext(destination, out routingContext)) {
            await routingContext.SendUnreliableAsync(
               destination,
               new MessageDto {
                  Body = payload,
                  ReceiverId = destination,
                  SenderId = identity.Id
               }).ConfigureAwait(false);
         }
      }

      public Task SendReliableAsync<T>(T payload, Guid destination) {
         IRoutingContext routingContext;
         if (routingTable.TryGetRoutingContext(destination, out routingContext)) {
            return routingContext.SendReliableAsync(
               destination,
               new MessageDto {
                  Body = payload,
                  ReceiverId = destination,
                  SenderId = identity.Id
               });
         } else {
            throw new InvalidStateException("Attempted reliable send peer with no routing context.");
         }
      }
   }
}
