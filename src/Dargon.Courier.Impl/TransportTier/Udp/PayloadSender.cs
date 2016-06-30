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
      private readonly IObjectPool<MemoryStream> outboundMemoryStreamPool = ObjectPool.Create(() => new MemoryStream(new byte[UdpConstants.kMaximumTransportSize], 0, UdpConstants.kMaximumTransportSize, true, true));
      private readonly UdpClient udpClient;

      public PayloadSender(UdpClient udpClient) {
         this.udpClient = udpClient;
      }

      public async Task SendAsync<T>(T payload) {
         var ms = outboundMemoryStreamPool.TakeObject();
       
//         var ms = new MemoryStream();
         Trace.Assert(ms.Position == 0);
         Serialize.To(ms, payload);
         ms.Position = 0;

         // Validate deserialize works
         try {
            var throwaway = Deserialize.From(ms);
         } catch (Exception e) {
            throw new AggregateException("Direct deserialize after serialize failed.", e);
         }

         await udpClient.BroadcastAsync(ms, 0, (int)ms.Position);

         ms.SetLength(0);
         outboundMemoryStreamPool.ReturnObject(ms);
      }
   }
}
