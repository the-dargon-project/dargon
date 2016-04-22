using System;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons.Collections;
using Dargon.Commons.Pooling;
using Dargon.Courier.Vox;
using Nito.AsyncEx;

namespace Dargon.Courier.PacketTier {
   public class AcknowledgementCoordinator {
      private readonly IObjectPool<AsyncAutoResetEvent> resetEventPool = ObjectPool.Create(() => new AsyncAutoResetEvent());
      private readonly ConcurrentDictionary<Guid, AsyncAutoResetEvent> resetEventsByAckId = new ConcurrentDictionary<Guid, AsyncAutoResetEvent>();

      public async Task Expect(Guid id, CancellationToken cancellationToken) {
         var sync = resetEventPool.TakeObject();
         try {
            if (!resetEventsByAckId.TryAdd(id, sync)) {
               throw new InvalidOperationException("Attempted to expect on taken guid.");
            }
            await sync.WaitAsync(cancellationToken);
         } finally {
            resetEventPool.ReturnObject(sync);
         }
      }

      public async Task ProcessAcknowledgementAsync(InboundPayloadEvent e) {
         await Task.Yield();
         var ack = (AcknowledgementDto)e.Payload;
         AsyncAutoResetEvent sync;
         if (resetEventsByAckId.TryRemove(ack.MessageId, out sync)) {
            sync.Set();
         }
      }
   }
}