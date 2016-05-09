using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Commons.Collections;
using Dargon.Commons.Pooling;
using Dargon.Courier.Vox;
using Dargon.Vox.Utilities;

namespace Dargon.Courier.AsyncPrimitives {
   public class AsyncRouter<TInput, TPassed> {
      private readonly ConcurrentTypeToDispatcherDictionary<TPassed> typeHandlers = new ConcurrentTypeToDispatcherDictionary<TPassed>();
      private readonly Func<TInput, Type> typeProjector;
      private readonly Func<TInput, TPassed> passedProjector;

      public AsyncRouter(Func<TInput, Type> typeProjector, Func<TInput, TPassed> passedProjector) {
         this.typeProjector = typeProjector;
         this.passedProjector = passedProjector;
      }

      public void RegisterHandler<T>(Func<TPassed, Task> handler) {
         var set = typeHandlers.GetOrAdd(typeof(T), add => new ConcurrentSet<Func<TPassed, Task>>());
         set.TryAdd(handler);
      }

      public async Task<bool> TryRouteAsync(TInput x) {
         var typeProjection = typeProjector(x);
         var passedProjection = passedProjector(x);
         ConcurrentSet<Func<TPassed, Task>> handlers;
         if (typeHandlers.TryGetValue(typeProjection, out handlers)) {
            await Task.WhenAll(handlers.Select(h => h(passedProjection)));
            return true;
         }
         return false;
      }

      public class ConcurrentTypeToDispatcherDictionary<TPassed> : IncrementalDictionary<Type, ConcurrentSet<Func<TPassed, Task>>> {

      }
   }

   public class InboundPayloadEventRouter : AsyncRouter<InboundPayloadEvent, InboundPayloadEvent> {
      public InboundPayloadEventRouter() : base(x => x.Payload.GetType(), x => x) { }
   }

   public class InboundPacketEventRouter : AsyncRouter<InboundPacketEvent, InboundPacketEvent> {
      public InboundPacketEventRouter() : base(x => x.Packet.Payload.GetType(), x => x) { }
   }

   public class NongenericInboundMessageToGenericDispatchInvoker {
      private delegate Task VisitAsyncFunc(InboundPacketEvent e, InboundMessageDispatcher dispatcher);
      private readonly IGenericFlyweightFactory<VisitAsyncFunc> visitorInvokers
         = GenericFlyweightFactory.ForMethod<VisitAsyncFunc>(
            typeof(DispatchVisitor<>), nameof(DispatchVisitor<object>.Visit));

      public Task InvokeDispatchAsync(InboundPacketEvent e, InboundMessageDispatcher dispatcher) {
         var message = (MessageDto)e.Packet.Payload;
         return visitorInvokers.Get(message.Body.GetType())(e, dispatcher);
      }

      private static class DispatchVisitor<T> {
         private static readonly IObjectPool<InboundMessageEvent<T>> pool = ObjectPool.Create(() => new InboundMessageEvent<T>());

         public static async Task Visit(InboundPacketEvent e, InboundMessageDispatcher dispatcher) {
            var inboundMessageEvent = pool.TakeObject();
            inboundMessageEvent.PacketEvent = e;

            await dispatcher.DispatchAsync(inboundMessageEvent);

            inboundMessageEvent.PacketEvent = null;
            pool.ReturnObject(inboundMessageEvent);
         }
      }
   }

   public class InboundMessageRouter {
      private readonly Inner inner = new Inner();

      public void RegisterHandler<T>(Func<InboundMessageEvent<T>, Task> handler) {
         inner.RegisterHandler<T>(x => handler((InboundMessageEvent<T>)x));
      }

      public Task RouteAsync<T>(InboundMessageEvent<T> x) {
         return inner.TryRouteAsync(x);
      }

      private class Inner : AsyncRouter<InternalRoutableInboundMessageEvent, InternalRoutableInboundMessageEvent> {
         public Inner() : base(x => x.Body.GetType(), x => x) {}
      }
   }
}
