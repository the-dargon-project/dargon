﻿using Dargon.Commons.AsyncPrimitives;
using Dargon.Commons.Pooling;
using Dargon.Courier.AuditingTier;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Vox2;

namespace Dargon.Courier.TransportTier.Tcp {
   public class PayloadUtils {
      private readonly IObjectPool<MemoryStream> memoryStreamPool = ObjectPool.CreateTlsBacked(() => new MemoryStream());
      private readonly VoxContext vox;
      private readonly IAuditAggregator<double> inboundBytesAggregator;
      private readonly IAuditAggregator<double> outboundBytesAggregator;

      public PayloadUtils(VoxContext vox, IAuditAggregator<double> inboundBytesAggregator, IAuditAggregator<double> outboundBytesAggregator) {
         this.vox = vox;
         this.inboundBytesAggregator = inboundBytesAggregator;
         this.outboundBytesAggregator = outboundBytesAggregator;
      }

      public async Task WritePayloadAsync(NetworkStream ns, object payload, AsyncLock writerLock, CancellationToken cancellationToken = default(CancellationToken)) {
         var ms = memoryStreamPool.TakeObject();
         // TODO: suspicious use of global serializer?
         using var vw = vox.CreateWriter(ms);
         vw.WritePolymorphic(payload);
         using (await writerLock.LockAsyncUnsafe_WithImportantCaveats(cancellationToken).ConfigureAwait(false)) {
            await WriteMemoryStreamAsync(ns, ms, 0, (int)ms.Position, cancellationToken).ConfigureAwait(false);
         }
         ms.SetLength(0);
         memoryStreamPool.ReturnObject(ms);
      }

      private async Task WriteMemoryStreamAsync(NetworkStream ns, MemoryStream ms, int offset, int length, CancellationToken cancellationToken = default(CancellationToken)) {
         await ns.WriteAsync(BitConverter.GetBytes(length), 0, sizeof(int), cancellationToken).ConfigureAwait(false);
         await ns.WriteAsync(ms.GetBuffer(), offset, length, cancellationToken).ConfigureAwait(false);
         outboundBytesAggregator.Put(sizeof(int) + length);
      }

      public async Task<object> ReadPayloadAsync(NetworkStream ns, CancellationToken cancellationToken) {
         var lengthBuffer = new byte[sizeof(int)];
         await ReadBytesAsync(ns, sizeof(int), lengthBuffer, cancellationToken).ConfigureAwait(false);

         var frameLength = BitConverter.ToInt32(lengthBuffer, 0);
         var frameBytes = await ReadBytesAsync(ns, frameLength, null, cancellationToken).ConfigureAwait(false);
         inboundBytesAggregator.Put(sizeof(int) + frameLength);

         using var ms = new MemoryStream(frameBytes, 0, frameBytes.Length, false, true);
         using var vr = vox.CreateReader(ms);
         return vr.ReadPolymorphic();
      }

      private async Task<byte[]> ReadBytesAsync(Stream stream, int count, byte[] buffer = null, CancellationToken cancellationToken = default(CancellationToken)) {
         buffer = buffer ?? new byte[count];
         int bytesRemaining = count;
         int totalBytesRead = 0;
         var spinner = new AsyncSpinner();
         while (bytesRemaining > 0) {
            int bytesRead = await stream.ReadAsync(buffer, totalBytesRead, bytesRemaining, cancellationToken).ConfigureAwait(false);
            bytesRemaining -= bytesRead;
            totalBytesRead += bytesRead;
            if (bytesRead == 0) {
               // TODO: Why is bytesRead sometimes 0? In that case ReadAsync returns 0 immediately.
               await spinner.SpinAsync();
            }
         }
         return buffer;
      }

   }
}
