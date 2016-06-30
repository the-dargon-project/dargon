using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons.Pooling;
using Dargon.Courier.AuditingTier;
using Dargon.Vox;
using Nito.AsyncEx;

namespace Dargon.Courier.TransportTier.Tcp {
   public class PayloadUtils {
      private readonly IObjectPool<MemoryStream> memoryStreamPool = ObjectPool.Create(() => new MemoryStream());
      private readonly AuditAggregator<double> inboundBytesAggregator;
      private readonly AuditAggregator<double> outboundBytesAggregator;

      public PayloadUtils(AuditAggregator<double> inboundBytesAggregator, AuditAggregator<double> outboundBytesAggregator) {
         this.inboundBytesAggregator = inboundBytesAggregator;
         this.outboundBytesAggregator = outboundBytesAggregator;
      }

      public async Task WritePayloadAsync(NetworkStream ns, object payload, AsyncLock writerLock, CancellationToken cancellationToken = default(CancellationToken)) {
         var ms = memoryStreamPool.TakeObject();
         Serialize.To(ms, payload);
         using (await writerLock.LockAsync()) {
            await WriteMemoryStreamAsync(ns, ms, 0, (int)ms.Position, cancellationToken);
         }
         ms.SetLength(0);
         memoryStreamPool.ReturnObject(ms);
      }

      private async Task WriteMemoryStreamAsync(NetworkStream ns, MemoryStream ms, int offset, int length, CancellationToken cancellationToken = default(CancellationToken)) {
         await ns.WriteAsync(BitConverter.GetBytes(length), 0, sizeof(int), cancellationToken);
         await ns.WriteAsync(ms.GetBuffer(), offset, length, cancellationToken);
         outboundBytesAggregator.Put(sizeof(int) + length);
      }

      public async Task<object> ReadPayloadAsync(NetworkStream ns, CancellationToken cancellationToken) {
         var lengthBuffer = new byte[sizeof(int)];
         await ReadBytesAsync(ns, sizeof(int), lengthBuffer, cancellationToken).ConfigureAwait(false);

         var frameLength = BitConverter.ToInt32(lengthBuffer, 0);
         var frameBytes = await ReadBytesAsync(ns, frameLength, null, cancellationToken);
         inboundBytesAggregator.Put(sizeof(int) + frameLength);

         return Deserialize.From(new MemoryStream(frameBytes, false));
      }

      private async Task<byte[]> ReadBytesAsync(Stream stream, int count, byte[] buffer = null, CancellationToken cancellationToken = default(CancellationToken)) {
         buffer = buffer ?? new byte[count];
         int bytesRemaining = count;
         int totalBytesRead = 0;
         while (bytesRemaining > 0) {
            int bytesRead = await stream.ReadAsync(buffer, totalBytesRead, bytesRemaining, cancellationToken).ConfigureAwait(false);
            bytesRemaining -= bytesRead;
            totalBytesRead += bytesRead;
         }
         return buffer;
      }

   }
}
