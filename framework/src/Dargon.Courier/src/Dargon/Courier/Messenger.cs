using Dargon.Commons.Collections;
using Dargon.Courier.RoutingTier;
using Dargon.Courier.TransportTier;
using Dargon.Courier.Vox;
using System;
using System.Linq;
using System.Threading.Tasks;
using Dargon.Commons.Exceptions;
using Dargon.Courier.PeeringTier;

namespace Dargon.Courier {
   public class Messenger {
      private readonly Identity identity;
      private readonly ConcurrentSet<ITransport> transports;
      private readonly RoutingTable routingTable;

      public Messenger(Identity identity, ConcurrentSet<ITransport> transports, RoutingTable routingTable) {
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
         return Task.WhenAll(transports.Select(t => t.SendMessageBroadcastAsync(message)).ToArray());
      }

      /// <summary>
      /// If a routing context cannot be found, successfully completes.
      /// </summary>
      public Task SendUnreliableAsync<T>(T payload, Guid destination) {
         IRoutingContext routingContext;
         if (!routingTable.TryGetRoutingContext(destination, out routingContext)) {
            return Task.CompletedTask;
         }

         return routingContext.SendUnreliableAsync(
            destination,
            new MessageDto {
               Body = payload,
               ReceiverId = destination,
               SenderId = identity.Id
            });
      }

      /// <summary>
      /// Immediately tries to look up a routing context and throws if one is not found.
      /// </summary>
      public Task SendReliableAsync<T>(T payload, Guid destination) {
         IRoutingContext routingContext;
         if (!routingTable.TryGetRoutingContext(destination, out routingContext)) {
            throw new InvalidStateException("Attempted reliable send peer with no routing context.");
         }

         return routingContext.SendReliableAsync(
            destination,
            new MessageDto {
               Body = payload,
               ReceiverId = destination,
               SenderId = identity.Id
            });
      }
   }

   public static class MessengerExtensions {
      public static Task SendUnreliableAsync<T>(this Messenger messenger, T payload, PeerContext peer)
         => messenger.SendUnreliableAsync(payload, peer.Identity.Id);

      public static Task SendUnreliableAsync<T>(this Messenger messenger, T payload, Identity peerIdentity)
         => messenger.SendUnreliableAsync(payload, peerIdentity.Id);

      public static Task SendReliableAsync<T>(this Messenger messenger, T payload, PeerContext peer)
         => messenger.SendReliableAsync(payload, peer.Identity.Id);

      public static Task SendReliableAsync<T>(this Messenger messenger, T payload, Identity peerIdentity)
         => messenger.SendReliableAsync(payload, peerIdentity.Id);

      public static Task SendAsync<T>(this Messenger messenger, T payload, Guid destination, bool isReliable)
         => isReliable ? messenger.SendReliableAsync(payload, destination) : messenger.SendUnreliableAsync(payload, destination);

      public static Task SendAsync<T>(this Messenger messenger, T payload, PeerContext peer, bool isReliable)
         => isReliable ? messenger.SendReliableAsync(payload, peer) : messenger.SendUnreliableAsync(payload, peer);

      public static Task SendAsync<T>(this Messenger messenger, T payload, Identity peerIdentity, bool isReliable)
         => isReliable ? messenger.SendReliableAsync(payload, peerIdentity) : messenger.SendUnreliableAsync(payload, peerIdentity);
   }
}
