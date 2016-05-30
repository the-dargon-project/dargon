using System.Threading.Tasks;
using Dargon.Commons.Pooling;

namespace Dargon.Courier {
   public class OutboundPayloadEventEmitter {
      private readonly IObjectPool<OutboundPayloadEvent> outboundPayloadEventPool = ObjectPool.Create(() => new OutboundPayloadEvent());
      private readonly IAsyncPoster<OutboundPayloadEvent> outboundPayloadEventPoster;

      public OutboundPayloadEventEmitter(IAsyncPoster<OutboundPayloadEvent> outboundPayloadEventPoster) {
         this.outboundPayloadEventPoster = outboundPayloadEventPoster;
      }

      public async Task EmitAsync(object payload, object tagEvent) {
         var outboundPayloadEvent = outboundPayloadEventPool.TakeObject();
         outboundPayloadEvent.Payload = payload;
         outboundPayloadEvent.TagEvent = tagEvent;

         await outboundPayloadEventPoster.PostAsync(outboundPayloadEvent);

         outboundPayloadEvent.Payload = null;
         outboundPayloadEvent.TagEvent = null;
         outboundPayloadEventPool.ReturnObject(outboundPayloadEvent);
      }
   }
}