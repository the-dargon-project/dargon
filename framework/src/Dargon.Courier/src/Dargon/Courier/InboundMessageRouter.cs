using System;
using System.Threading.Tasks;
using Dargon.Commons.AsyncPrimitives;
using Dargon.Courier.AccessControlTier;

namespace Dargon.Courier {
   public class InboundMessageRouter {
      private readonly Inner inner = new Inner();
      private readonly IGatekeeper gatekeeper;

      public InboundMessageRouter(IGatekeeper gatekeeper) {
         this.gatekeeper = gatekeeper;
      }

      public void RegisterHandler<T>(Func<IInboundMessageEvent<T>, Task> handler) {
         inner.RegisterHandler<T>(x => handler((IInboundMessageEvent<T>)x));
      }

      public Task<bool> TryRouteAsync<T>(InboundMessageEvent<T> x) {
         gatekeeper.ValidateInboundMessageEvent<T>(x);
         return inner.TryRouteAsync(x);
      }

      private class Inner : AsyncRouter<InternalRoutableInboundMessageEvent, InternalRoutableInboundMessageEvent> {
         public Inner() : base(x => x.Body.GetType(), x => x) { }
      }
   }
}