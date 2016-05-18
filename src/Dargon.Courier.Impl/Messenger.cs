using Dargon.Courier.Vox;
using System;
using System.Threading.Tasks;
using Dargon.Commons.Pooling;

namespace Dargon.Courier {
   public class Messenger {
      private readonly IObjectPool<OutboundMessageEvent> outboundMessageEventPool;
      private readonly IAsyncPoster<OutboundMessageEvent> outboundMessageEventPoster;

      public Messenger(IObjectPool<OutboundMessageEvent> outboundMessageEventPool, IAsyncPoster<OutboundMessageEvent> outboundMessageEventPoster) {
         this.outboundMessageEventPool = outboundMessageEventPool;
         this.outboundMessageEventPoster = outboundMessageEventPoster;
      }

      public Task BroadcastAsync<T>(T payload) {
         return HelperAsync(payload, Guid.Empty, false);
      }

      public Task SendAsync<T>(T payload, Guid destination) {
         return HelperAsync(payload, destination, false);
      }

      public Task SendReliableAsync<T>(T payload, Guid destination) {
         return HelperAsync(payload, destination, true);
      }

      private async Task HelperAsync(object payload, Guid destination, bool reliable) {
         var e = outboundMessageEventPool.TakeObject();
         e.Message.Body = payload;
         e.Message.ReceiverId = destination;
         e.Reliable = reliable;

         await outboundMessageEventPoster.PostAsync(e).ConfigureAwait(false);

         e.Message.Body = null;
         e.Message.ReceiverId = Guid.Empty;
         e.Reliable = false;
         outboundMessageEventPool.ReturnObject(e);
      }
   }
}
