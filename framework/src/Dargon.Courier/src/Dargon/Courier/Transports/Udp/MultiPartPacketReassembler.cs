using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Commons.AsyncPrimitives;
using Dargon.Commons.Pooling;
using Dargon.Commons.Utilities;
using Dargon.Courier.TransportTier.Udp.Vox;
using NLog;
using static Dargon.Commons.Channels.ChannelsExtensions;

namespace Dargon.Courier.TransportTier.Udp {
   public class MultiPartPacketReassembler {
      private static readonly Logger logger = LogManager.GetCurrentClassLogger();

      private static readonly TimeSpan ChunkAssemblyTimeoutInterval = TimeSpan.FromMinutes(5);

      private readonly ConcurrentDictionary<Guid, ChunkReassemblyContext> reassemblyContextsByMessageId = new();

      private IUdpDispatcher dispatcher;

      public void SetUdpDispatcher(IUdpDispatcher dispatcher) {
         this.dispatcher = dispatcher;
      }

      public async Task HandleInboundMultiPartChunkAsync(IOpaqueUdpNetworkAdapter adapter, MultiPartChunkDto chunk, UdpRemoteInfo remoteInfo) {
         var newReassemblyContext = new ChunkReassemblyContext {
            Chunks = new MultiPartChunkDto[chunk.ChunkCount],
            ChunksRemaining = chunk.ChunkCount,
         };
         
         var reassemblyContext = reassemblyContextsByMessageId.GetOrAdd(chunk.MultiPartMessageId, newReassemblyContext);
         if (reassemblyContext == newReassemblyContext) {
            Go(async () => {
               await Task.Delay(ChunkAssemblyTimeoutInterval);
               reassemblyContextsByMessageId.TryRemove(chunk.MultiPartMessageId, out _);
            });
         }

         Interlocked2.Write(ref reassemblyContext.Chunks[chunk.ChunkIndex], chunk);
         var chunksRemaining = Interlocked2.PostDecrement(ref reassemblyContext.ChunksRemaining);
         if (chunksRemaining == 0) {
            reassemblyContextsByMessageId.TryRemove(chunk.MultiPartMessageId, out _);
            ReassembleChunksAndDispatch(adapter, reassemblyContext.Chunks, remoteInfo);
         }
      }

      private void ReassembleChunksAndDispatch(IOpaqueUdpNetworkAdapter adapter, MultiPartChunkDto[] chunks, UdpRemoteInfo remoteInfo) {
         var payloadLength = 0;
         foreach (var c in chunks) payloadLength += c.BodyLength;
         
         var lbv = CoreUdp.AcquireLeasedBufferView();
         lbv.SetDataRange(0, payloadLength);
         
         for (int i = 0, offset = 0; i < chunks.Length; i++) {
            var chunk = chunks[i];
            var src = chunk.Body.AsSpan(chunk.BodyOffset, chunk.BodyLength);
            var dst = lbv.Span.Slice(offset, chunk.BodyLength);
            src.CopyTo(dst);
            offset += chunk.BodyLength;
         }

         dispatcher.HandleInboundUdpPacket(adapter, lbv.Transfer, remoteInfo);
      }
   }

   public class ChunkReassemblyContext {
      public MultiPartChunkDto[] Chunks;
      public int ChunksRemaining;
   }
}
