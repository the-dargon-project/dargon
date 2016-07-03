using System;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Commons.Collections;
using Dargon.Commons.Pooling;
using Dargon.Courier.AsyncPrimitives;
using Dargon.Courier.TransportTier.Udp.Vox;
using Nito.AsyncEx;

namespace Dargon.Courier.TransportTier.Udp {
   public class AcknowledgementCoordinator {
      private readonly ConcurrentDictionary<Guid, AsyncLatch> signalsByAckId = new ConcurrentDictionary<Guid, AsyncLatch>();

      public async Task Expect(Guid id, CancellationToken cancellationToken) {
         var sync = new AsyncLatch();
         signalsByAckId.AddOrThrow(id, sync);
         await sync.WaitAsync(cancellationToken).ConfigureAwait(false);
      }

      public void ProcessAcknowledgement(Guid id) {
         AsyncLatch sync;
         if (signalsByAckId.TryRemove(id, out sync)) {
            Interlocked.Increment(ref DebugRuntimeStats.out_rs_acked);
            sync.Set();
         }
      }
   }
}