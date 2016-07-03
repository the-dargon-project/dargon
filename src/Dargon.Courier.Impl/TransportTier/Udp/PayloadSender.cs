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
   public class PayloadSender {
      private readonly IObjectPool<MemoryStream> outboundMemoryStreamPool = ObjectPool.CreateStackBacked(() => new MemoryStream(new byte[UdpConstants.kMaximumTransportSize], 0, UdpConstants.kMaximumTransportSize, true, true));
      private readonly UdpClient udpClient;

      public PayloadSender(UdpClient udpClient) {
         this.udpClient = udpClient;
      }

      public async Task SendAsync<T>(T payload) {
         Interlocked.Increment(ref DebugRuntimeStats.out_ps);
         var ms = outboundMemoryStreamPool.TakeObject();
       
//         var ms = new MemoryStream();
         Trace.Assert(ms.Position == 0);
         Serialize.To(ms, payload);
//         // Validate deserialize works
//         ms.Position = 0;
//         try {
//            var throwaway = Deserialize.From(ms);
//         } catch (Exception e) {
//            throw new AggregateException("Direct deserialize after serialize failed.", e);
//         }

         await udpClient.BroadcastAsync(ms, 0, (int)ms.Position).ConfigureAwait(false);

         ms.SetLength(0);
         outboundMemoryStreamPool.ReturnObject(ms);
         Interlocked.Increment(ref DebugRuntimeStats.out_ps_done);
      }
   }
}
