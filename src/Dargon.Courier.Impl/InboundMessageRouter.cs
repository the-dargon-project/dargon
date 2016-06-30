using System;
using System.Threading.Tasks;
using Dargon.Courier.AsyncPrimitives;

namespace Dargon.Courier {
   public class InboundMessageRouter {
      private readonly Inner inner = new Inner();

      public void RegisterHandler<T>(Func<IInboundMessageEvent<T>, Task> handler) {
         inner.RegisterHandler<T>(x => handler((IInboundMessageEvent<T>)x));
      }

      public Task<bool> TryRouteAsync<T>(InboundMessageEvent<T> x) {
         return inner.TryRouteAsync(x);
      }

      private class Inner : AsyncRouter<InternalRoutableInboundMessageEvent, InternalRoutableInboundMessageEvent> {
         public Inner() : base(x => x.Body.GetType(), x => x) { }
      }
   }
}