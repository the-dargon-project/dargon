using System;
using System.Collections.Generic;
using System.Diagnostics;
using Dargon.Commons.Pooling;
using Dargon.Vox;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Courier.TransportTier.Udp.Vox;

namespace Dargon.Courier.TransportTier.Udp {
   public class CoreBroadcaster {
      // TODO: Figure out better pooling
      private readonly IObjectPool<MemoryStream> outboundMemoryStreamPool = ObjectPool.CreateConcurrentQueueBacked(() => new MemoryStream(new byte[UdpConstants.kMaximumTransportSize], 0, UdpConstants.kMaximumTransportSize, true, true));
      private readonly CoreUdp coreUdp;

      public CoreBroadcaster(CoreUdp coreUdp) {
         this.coreUdp = coreUdp;
      }

      public async Task BroadcastAsync<T>(T payload) {
#if DEBUG
         Interlocked.Increment(ref DebugRuntimeStats.out_ps);
#endif
         var ms = outboundMemoryStreamPool.TakeObject();
       
         Trace.Assert(ms.Position == 0);
         await AsyncSerialize.ToAsync(ms, payload).ConfigureAwait(false);

         coreUdp.Broadcast(
            ms, 0, (int)ms.Position,
            () => {
               ms.SetLength(0);
               outboundMemoryStreamPool.ReturnObject(ms);
#if DEBUG
               Interlocked.Increment(ref DebugRuntimeStats.out_ps_done);
#endif
            });
      }
   }
}
