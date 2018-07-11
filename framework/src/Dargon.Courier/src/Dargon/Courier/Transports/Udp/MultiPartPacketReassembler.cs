using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Commons.Pooling;
using Dargon.Courier.TransportTier.Udp.Vox;
using NLog;
using static Dargon.Commons.Channels.ChannelsExtensions;

namespace Dargon.Courier.TransportTier.Udp {
   public class MultiPartPacketReassembler {
      private static readonly Logger logger = LogManager.GetCurrentClassLogger();

      private static readonly TimeSpan kSomethingExpiration = TimeSpan.FromMinutes(5);

      private readonly ConcurrentDictionary<Guid, ChunkReassemblyContext> chunkReassemblerContextsByMessageId = new ConcurrentDictionary<Guid, ChunkReassemblyContext>();
      private readonly IObjectPool<InboundDataEvent> inboundDataEventPool = ObjectPool.CreateTlsBacked(() => new InboundDataEvent());
      private IUdpDispatcher dispatcher;

      public void SetUdpDispatcher(IUdpDispatcher dispatcher) {
         this.dispatcher = dispatcher;
      }

      public void HandleInboundMultiPartChunk(MultiPartChunkDto chunk) {
         bool isAdded = false;
         ChunkReassemblyContext addedChunkReassemblyContext = null;
         var chunkReassemblyContext = chunkReassemblerContextsByMessageId.GetOrAdd(
            chunk.MultiPartMessageId,
            add => {
//               logger.Info(Thread.CurrentThread.ManagedThreadId + ": " + "NEW " + chunk.MultiPartMessageId + " " + this.GetHashCode());

               isAdded = true;
               return addedChunkReassemblyContext = new ChunkReassemblyContext(chunk.ChunkCount);
            });

//         if (isAdded) {
//            logger.Info(Thread.CurrentThread.ManagedThreadId + ": " + chunkReassemblyContext.GetHashCode() + " " + new ChunkReassemblyContext(0).GetHashCode() + "");
//         }

         if (chunkReassemblyContext == addedChunkReassemblyContext) {
            Go(async () => {
               await Task.Delay(kSomethingExpiration).ConfigureAwait(false);

               RemoveAssemblerFromCache(chunk.MultiPartMessageId);
            });
         }

         var completedChunks = chunkReassemblyContext.AddChunk(chunk);
         if (completedChunks != null) {
            ReassembleChunksAndDispatch(completedChunks);
         }
      }

      private void ReassembleChunksAndDispatch(IReadOnlyList<MultiPartChunkDto> chunks) {
//         Console.WriteLine(chunks.First().MultiPartMessageId.ToString("n").Substring(0, 6) + " Got to reassemble!");
         RemoveAssemblerFromCache(chunks.First().MultiPartMessageId);

         var payloadLength = chunks.Sum(c => c.BodyLength);
         var payloadBytes = new byte[payloadLength];
         for (int i = 0, offset = 0; i < chunks.Count; i++) {
            var chunk = chunks[i];
            Buffer.BlockCopy(chunk.Body, 0, payloadBytes, offset, chunk.BodyLength);
            offset += chunk.BodyLength;
         }

         var e = inboundDataEventPool.TakeObject();
         e.Data = payloadBytes;
         e.DataOffset = 0;
         e.DataLength = payloadLength;

//         Console.WriteLine(chunks.First().MultiPartMessageId.ToString("n").Substring(0, 6) + " Dispatching to HIDE!");
         dispatcher.HandleInboundDataEvent(
            e,
            _ => {
               e.Data = null;
               inboundDataEventPool.ReturnObject(e);
            });
      }

      private void RemoveAssemblerFromCache(Guid multiPartMessageId) {
         ChunkReassemblyContext throwaway;
         chunkReassemblerContextsByMessageId.TryRemove(multiPartMessageId, out throwaway);
      }
   }

   public class ChunkReassemblyContext {
      private readonly MultiPartChunkDto[] x;
      private int chunksRemaining;

      public ChunkReassemblyContext(int chunkCount) {
         x = new MultiPartChunkDto[chunkCount];
         chunksRemaining = chunkCount;
      }

      public MultiPartChunkDto[] AddChunk(MultiPartChunkDto chunk) {
         if (Interlocked.CompareExchange(ref x[chunk.ChunkIndex], chunk, null) == null) {
            var newChunksRemaining = Interlocked.Decrement(ref chunksRemaining);
            if (newChunksRemaining < 100) {
//               Console.WriteLine(chunk.MultiPartMessageId.ToString("n").Substring(0, 6) + " MPP REMAINING " + newChunksRemaining);
            }
            if (newChunksRemaining == 0) {
               return x;
            }
         }
         return null;
      }
   }
}
