﻿using System;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Commons.Pooling;
using Dargon.Courier.PeeringTier;
using Dargon.Courier.Vox;
using Dargon.Vox.Utilities;
using NLog;

namespace Dargon.Courier {
   public class InboundMessageDispatcher {
      private static readonly Logger logger = LogManager.GetCurrentClassLogger();

      private readonly Identity identity;
      private readonly PeerTable peerTable;
      private readonly InboundMessageRouter inboundMessageRouter;

      public InboundMessageDispatcher(Identity identity, PeerTable peerTable, InboundMessageRouter inboundMessageRouter) {
         this.identity = identity;
         this.peerTable = peerTable;
         this.inboundMessageRouter = inboundMessageRouter;
      }

      public async Task DispatchAsync(MessageDto message) {
         await TaskEx.YieldToThreadPool();

         bool a = identity.Matches(message.SenderId, IdentityMatchingScope.LocalIdentity);
         bool b = !identity.Matches(message.ReceiverId, IdentityMatchingScope.Broadcast);
         if (a || b) {
            return;
         }

         PeerContext peerContext = null;
         if (message.SenderId != Guid.Empty) {
            peerContext = peerTable.GetOrAdd(message.SenderId);
            await peerContext.WaitForDiscoveryAsync().ConfigureAwait(false);
         }

         await RouteAsyncVisitor.Visit(inboundMessageRouter, message, peerContext).ConfigureAwait(false);
      }

      private static class RouteAsyncVisitor {
         private delegate Task VisitorFunc(InboundMessageRouter router, MessageDto message, PeerContext peer);

         private static readonly IGenericFlyweightFactory<VisitorFunc> visitorFactory
            = GenericFlyweightFactory.ForMethod<VisitorFunc>(
               typeof(Inner<>),
               nameof(Inner<object>.Visit));

         public static Task Visit(InboundMessageRouter router, MessageDto message, PeerContext peer) {
            return visitorFactory.Get(message.Body.GetType())(router, message, peer);
         }

         private static class Inner<T> {
            private static readonly IObjectPool<InboundMessageEvent<T>> eventPool = ObjectPool.CreateStackBacked(() => new InboundMessageEvent<T>());

            public static async Task Visit(InboundMessageRouter router, MessageDto message, PeerContext peer) {
               var e = eventPool.TakeObject();
               e.Message = message;
               e.Sender = peer;

               if (!await router.TryRouteAsync(e).ConfigureAwait(false)) {
                  logger.Trace($"Failed to route inbound message of body type {e.Body?.GetType().Name ?? "[null]"}");
               }

               e.Message = null;
               eventPool.ReturnObject(e);
            }
         }
      }
   }
}