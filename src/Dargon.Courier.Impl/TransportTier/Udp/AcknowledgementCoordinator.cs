using System;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Commons.Collections;
using Dargon.Commons.Pooling;
using Dargon.Courier.TransportTier.Udp.Vox;
using Nito.AsyncEx;

namespace Dargon.Courier.TransportTier.Udp {
   public class AcknowledgementCoordinator {
      private readonly IObjectPool<AsyncAutoResetEvent> resetEventPool = ObjectPool.CreateConcurrentQueueBacked(() => new AsyncAutoResetEvent());
      private readonly ConcurrentDictionary<Guid, AsyncAutoResetEvent> resetEventsByAckId = new ConcurrentDictionary<Guid, AsyncAutoResetEvent>();

      public async Task Expect(Guid id, CancellationToken cancellationToken) {
         var sync = resetEventPool.TakeObject();
         try {
            if (!resetEventsByAckId.TryAdd(id, sync)) {
               throw new InvalidOperationException("Attempted to expect on taken guid.");
            }
            await sync.WaitAsync(cancellationToken).ConfigureAwait(false);
         } finally {
            resetEventPool.ReturnObject(sync);
         }
      }

      public async Task ProcessAcknowledgementAsync(Guid id) {
         await TaskEx.YieldToThreadPool();
         AsyncAutoResetEvent sync;
         if (resetEventsByAckId.TryRemove(id, out sync)) {
            sync.Set();
         }
      }
   }
}