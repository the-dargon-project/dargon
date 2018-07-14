using Dargon.Commons;
using Dargon.Commons.AsyncPrimitives;
using Dargon.Commons.Collections;
using Dargon.Commons.Pooling;
using Dargon.Courier.AuditingTier;
using Dargon.Courier.TransportTier.Udp.Vox;
using Dargon.Courier.Vox;
using Dargon.Vox;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons.Exceptions;
using NLog;
using static Dargon.Commons.Channels.ChannelsExtensions;

namespace Dargon.Courier.TransportTier.Udp {
   public class UdpBroadcaster {
      // todo: figure out better pooling
      private readonly IObjectPool<MemoryStream> broadcastOutboundMemoryStreamPool = ObjectPool.CreateConcurrentQueueBacked(() => new MemoryStream(new byte[UdpConstants.kMaximumTransportSize], 0, UdpConstants.kMaximumTransportSize, true, true));
      private readonly Identity identity;
      private readonly UdpClient udpClient;

      public UdpBroadcaster(Identity identity, UdpClient udpClient) {
         this.identity = identity;
         this.udpClient = udpClient;
      }

      public async Task SendBroadcastAsync(MessageDto message) {
         var packet = PacketDto.Create(
            identity.Id,
            Guid.Empty,
            message,
            false);

         var ms = broadcastOutboundMemoryStreamPool.TakeObject();
         ms.SetLength(0);
         try {
            await AsyncSerialize.ToAsync(ms, packet).ConfigureAwait(false);
         } catch (NotSupportedException) {
            throw new InvalidOperationException("Broadcast message would surpass Courier Maximum UDP Transport Size");
         } 

         udpClient.Broadcast(
            ms, 0, (int)ms.Position,
            () => {
               ms.SetLength(0);
               broadcastOutboundMemoryStreamPool.ReturnObject(ms);
            });
      }
   }

   public static class AsyncSerialize {
      static AsyncSerialize() {
         var workerCount = Math.Max(4, Environment.ProcessorCount);
         requestQueues = Arrays.Create(workerCount, i => new ConcurrentQueue<Tuple<MemoryStream, object, TaskCompletionSource<byte>>>());
         requestSemaphores = Arrays.Create(workerCount, i => new Semaphore(0, int.MaxValue));
         for (var i = 0; i < workerCount; i++) {
            var capture = i;
            new Thread(() => ProcessorThreadStart(requestQueues[capture], requestSemaphores[capture])) {
               IsBackground = true,
               Name = $"AsyncSerialize_{i}"
            }.Start();
         }
      }

      private static readonly ConcurrentQueue<Tuple<MemoryStream, object, TaskCompletionSource<byte>>>[] requestQueues;
      private static readonly Semaphore[] requestSemaphores;
      [ThreadStatic] private static uint roundRobinCounter;

      private static void ProcessorThreadStart(ConcurrentQueue<Tuple<MemoryStream, object, TaskCompletionSource<byte>>> requestQueue, Semaphore requestSemaphore) {
         while (true) {
            requestSemaphore.WaitOne();
            Tuple<MemoryStream, object, TaskCompletionSource<byte>> r;
            if (!requestQueue.TryDequeue(out r)) {
               throw new InvalidStateException();
            }
            try {
               // TODO: suspicious use of global serializer?
               Serialize.To(r.Item1, r.Item2);
               r.Item3.SetResult(0);
            } catch (Exception e) {
               r.Item3.SetException(e);
            }
         }
      }

      public static Task ToAsync(MemoryStream ms, object x) {
         var tcs = new TaskCompletionSource<byte>(TaskCreationOptions.RunContinuationsAsynchronously);
         var i = (roundRobinCounter++) % requestQueues.Length;
         requestQueues[i].Enqueue(Tuple.Create(ms, x, tcs));
         requestSemaphores[i].Release();
         return tcs.Task;
      }
   }

   public static class AsyncClone {
      static AsyncClone() {
         var workerCount = Math.Max(4, Environment.ProcessorCount);
         requestQueues = Arrays.Create(workerCount, i => new ConcurrentQueue<Tuple<object, TaskCompletionSource<object>>>());
         requestSemaphores = Arrays.Create(workerCount, i => new Semaphore(0, int.MaxValue));
         for (var i = 0; i < workerCount; i++) {
            var capture = i;
            new Thread(() => ProcessorThreadStart(requestQueues[capture], requestSemaphores[capture])) { IsBackground = true, Name = $"AsyncClone_{i}" }.Start();
         }
      }

      private static readonly ConcurrentQueue<Tuple<object, TaskCompletionSource<object>>>[] requestQueues;
      private static readonly Semaphore[] requestSemaphores;
      [ThreadStatic] private static uint roundRobinCounter;

      private static void ProcessorThreadStart(ConcurrentQueue<Tuple<object, TaskCompletionSource<object>>> requestQueue, Semaphore requestSemaphore) {
         while (true) {
            requestSemaphore.WaitOne();
            Tuple<object, TaskCompletionSource<object>> r;
            if (!requestQueue.TryDequeue(out r)) {
               throw new InvalidStateException();
            }
            try {
               r.Item2.SetResult(r.Item1.DeepCloneSerializable());
            } catch (Exception e) {
               r.Item2.SetException(e);
            }
         }
      }

      public static async Task<T> CloneAsync<T>(T x) {
         var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
         var i = (roundRobinCounter++) % requestQueues.Length;
         requestQueues[i].Enqueue(Tuple.Create((object)x, tcs));
         requestSemaphores[i].Release();
         return (T)await tcs.Task.ConfigureAwait(false);
      }
   }

   public class UdpUnicaster {
      private static readonly Logger logger = LogManager.GetCurrentClassLogger();

      private readonly AsyncAutoResetLatch workSignal = new AsyncAutoResetLatch();
      private readonly IObjectPool<MemoryStream> outboundAcknowledgementMemoryStreamPool = ObjectPool.CreateConcurrentQueueBacked(() => new MemoryStream(new byte[UdpConstants.kAckSerializationBufferSize], 0, UdpConstants.kAckSerializationBufferSize, true, true)); // todo: figure out better pooling
      private readonly IObjectPool<MemoryStream> outboundPacketMemoryStreamPool; // todo: figure out better pooling
      private readonly Identity identity;
      private readonly UdpClient udpClient;
      private readonly AcknowledgementCoordinator acknowledgementCoordinator;
      private readonly UdpClientRemoteInfo remoteInfo;
      private readonly ConcurrentQueue<SendRequest> unhandledSendRequestQueue = new ConcurrentQueue<SendRequest>();
      private readonly IAuditCounter resendsCounter;
      private readonly IAuditAggregator<int> resendsAggregator;
      private readonly IAuditAggregator<double> outboundMessageRateLimitAggregator;
      private readonly IAuditAggregator<double> sendQueueDepthAggregator;

      public class ByteArrayPoolBackedMemoryStreamPool : IObjectPool<MemoryStream> {
         public ByteArrayPoolBackedMemoryStreamPool(IObjectPool<byte[]> backingPool) {
            BackingPool = backingPool;
         }

         public IObjectPool<byte[]> BackingPool { get; }
         public string Name => BackingPool.Name + "_ms";
         public int Count => BackingPool.Count;

         public MemoryStream TakeObject() {
            var buffer = BackingPool.TakeObject();
            return new MemoryStream(buffer, 0, buffer.Length, true, true);
         }
         
         public void ReturnObject(MemoryStream item) {
            BackingPool.ReturnObject(item.GetBuffer());
         }
      }

      public UdpUnicaster(Identity identity, UdpClient udpClient, AcknowledgementCoordinator acknowledgementCoordinator, IObjectPool<byte[]> sendReceiveBufferPool, UdpClientRemoteInfo remoteInfo, IAuditCounter resendsCounter, IAuditAggregator<int> resendsAggregator, IAuditAggregator<double> outboundMessageRateLimitAggregator, IAuditAggregator<double> sendQueueDepthAggregator) {
         this.outboundPacketMemoryStreamPool = new ByteArrayPoolBackedMemoryStreamPool(sendReceiveBufferPool);
         this.identity = identity;
         this.udpClient = udpClient;
         this.acknowledgementCoordinator = acknowledgementCoordinator;
         this.remoteInfo = remoteInfo;
         this.resendsCounter = resendsCounter;
         this.resendsAggregator = resendsAggregator;
         this.outboundMessageRateLimitAggregator = outboundMessageRateLimitAggregator;
         this.sendQueueDepthAggregator = sendQueueDepthAggregator;
      }

      public void Initialize() {
         RunMessageQueueProcessorAsync().Forget();

         // Periodically signal work is available as resends don't signal.
         Go(async () => {
            while (true) {
               workSignal.Set();
               await Task.Delay(100).ConfigureAwait(false);
            }
         }).Forget();
      }

      private async Task RunMessageQueueProcessorAsync() {
         const int rateLimiterBaseRate = 10000;
         const int rateLimiterRateVelocity = 2000;
         const int rateLimiterInitialRate = 100000;
         var rateLimiter = new OutboundMessageRateLimiter(
            outboundMessageRateLimitAggregator,
            rateLimiterBaseRate,
            rateLimiterRateVelocity,
            rateLimiterInitialRate,
            0.9);

         var inTransportSendRequestQueue = new ConcurrentQueue<SendRequest>();
         while (true) {
            // Block until work available is signalled, then compute sends remaining from rate limiter
            await workSignal.WaitAsync().ConfigureAwait(false);
            sendQueueDepthAggregator.Put(unhandledSendRequestQueue.Count);
            var sendsRemaining = rateLimiter.TakeAndResetOutboundMessagesAvailableCounter();

            // TODO: Analytics on these two things. Pool miss/hit/depth would be useful in general.
            // Console.WriteLine(outboundAcknowledgementMemoryStreamPool.Count + " " + outboundPacketMemoryStreamPool.Count);

            // The general algorithm consists of three queues:
            //   1. In-Transport Queue - sends yet to be acknowledged
            //   2. Request Queue - haven't been sent out yet
            //   3. Pending Reenqueue Queue - To be readded to in-transport queue
            // 
            // The in-transport queue is iterated through. Iterated elements
            // are either forgotten in response to acknowledgement, resent
            // presumably in response to packet loss, or thrown into the pending
            // reenqueue queue otherwise. 
            //
            // Then, the request queue may be processed. Unreliable messages are
            // forgotten after send while reliable messages are thrown into
            // the pending renqueue queue.
            //
            // Finally, the pending reenqueue queue is thrown back into the in-
            // transport queue.

            var loopStartTime = DateTime.Now;
            bool droppedPacketDetected = false;
            var pendingRenqueues = new List<SendRequest>();
            var outboundSendRequests = new List<SendRequest>();
            SendRequest sendRequest;
            while (sendsRemaining > 0 &&
                   (inTransportSendRequestQueue.TryDequeue(out sendRequest) ||
                    unhandledSendRequestQueue.TryDequeue(out sendRequest))) {
               if (!sendRequest.IsReliable) {
                  sendsRemaining--;
                  outboundSendRequests.Add(sendRequest);
               } else {
                  if (sendRequest.AcknowledgementSignal.IsSet()) {
                     resendsAggregator.Put(sendRequest.SendCount - 1);
                     sendRequest.Data.SetLength(0);
                     sendRequest.DataBufferPool.ReturnObject(sendRequest.Data);
                     sendRequest.CompletionSignal.SetOrThrow();
#if DEBUG
                     Interlocked.Increment(ref DebugRuntimeStats.out_rs_done);
#endif
                     continue;
                  }
                  if (loopStartTime < sendRequest.NextSendTime) {
                     pendingRenqueues.Add(sendRequest);
                  } else {
                     sendsRemaining--;
                     if (sendRequest.SendCount > 0) {
                        resendsCounter.Increment();
                        droppedPacketDetected = true;
                     }

#if DEBUG
                     Interlocked.Increment(ref DebugRuntimeStats.out_sent);
#endif
                     outboundSendRequests.Add(sendRequest);
                  }
               }
            }

            if (outboundSendRequests.Any()) {
               udpClient.Unicast(
                  remoteInfo,
                  outboundSendRequests.Map(sr => sr.Data),
                  () => {
                     var now = DateTime.Now;
                     foreach (var outboundSendRequest in outboundSendRequests) {
                        if (!outboundSendRequest.IsReliable) {
#if DEBUG
                           Interlocked.Increment(ref DebugRuntimeStats.out_sent);
#endif
                           outboundSendRequest.Data.SetLength(0);
                           outboundSendRequest.DataBufferPool.ReturnObject(outboundSendRequest.Data);
                        } else {
                           const int kResendIntervalBase = 2048;
                           var maxResendDelayMillis = Math.Min(8000, kResendIntervalBase * (1 << (outboundSendRequest.SendCount >> 3)));
                           var actualResendDelayMillis = maxResendDelayMillis - StaticRandom.Next(maxResendDelayMillis / 4);
                           outboundSendRequest.NextSendTime = now + TimeSpan.FromMilliseconds(actualResendDelayMillis);
                           outboundSendRequest.SendCount++;
                           inTransportSendRequestQueue.Enqueue(outboundSendRequest);
                        }
                     }
                  });
            }

            pendingRenqueues.ForEach(inTransportSendRequestQueue.Enqueue);
            if (droppedPacketDetected) {
               rateLimiter.HandlePacketLoss();
            }
         }
      }

      public async Task SendAcknowledgementAsync(Guid destination, AcknowledgementDto acknowledgement) {
         var ms = outboundAcknowledgementMemoryStreamPool.TakeObject();
         ms.SetLength(0);
         await AsyncSerialize.ToAsync(ms, acknowledgement).ConfigureAwait(false);
         unhandledSendRequestQueue.Enqueue(
            new SendRequest {
               Data = ms,
               DataOffset = 0,
               DataLength = (int)ms.Position,
               DataBufferPool = outboundAcknowledgementMemoryStreamPool,
               IsReliable = false
            });
         workSignal.Set();
      }

      public Task SendReliableAsync(Guid destination, MessageDto message) {
#if DEBUG
         Interlocked.Increment(ref DebugRuntimeStats.out_rs);
#endif
         return SendHelperAsync(destination, message, true);
         //         Interlocked.Increment(ref DebugRuntimeStats.out_rs_done);
      }

      public async Task SendUnreliableAsync(Guid destination, MessageDto message) {
#if DEBUG
         Interlocked.Increment(ref DebugRuntimeStats.out_nrs);
#endif
         await SendHelperAsync(destination, message, false).ConfigureAwait(false);
#if DEBUG
         Interlocked.Increment(ref DebugRuntimeStats.out_nrs_done);
#endif
      }

      private async Task SendHelperAsync(Guid destination, MessageDto message, bool reliable) {
         var packetDto = PacketDto.Create(identity.Id, destination, message, reliable);
         var ms = outboundPacketMemoryStreamPool.TakeObject();
         ms.SetLength(0);
         try {
            await AsyncSerialize.ToAsync(ms, packetDto).ConfigureAwait(false);
         } catch (NotSupportedException) {
            // surpassed memory stream buffer size.
            if (!reliable) {
               throw new InvalidOperationException("Attempted to send nonreliable buffer surpassing transport buffer size");
            }

            ms.SetLength(0);
            outboundPacketMemoryStreamPool.ReturnObject(ms);
            await SendReliableMultipartPacketAsync(destination, packetDto).ConfigureAwait(false);
#if DEBUG
            Interlocked.Increment(ref DebugRuntimeStats.out_rs_done);
#endif
            return;
         }

         if (reliable) {
            var completionSignal = new AsyncLatch();
            var acknowlewdgementSignal = acknowledgementCoordinator.Expect(packetDto.Id);
            unhandledSendRequestQueue.Enqueue(
               new SendRequest {
                  DataBufferPool = outboundPacketMemoryStreamPool,
                  Data = ms,
                  DataOffset = 0,
                  DataLength = (int)ms.Position,
                  IsReliable = true,
                  AcknowledgementSignal = acknowlewdgementSignal,
                  AcknowledgementGuid = packetDto.Id,
                  CompletionSignal = completionSignal
               });
            workSignal.Set();
            await completionSignal.WaitAsync().ConfigureAwait(false);
         } else {
            unhandledSendRequestQueue.Enqueue(
               new SendRequest {
                  DataBufferPool = outboundPacketMemoryStreamPool,
                  Data = ms,
                  DataOffset = 0,
                  DataLength = (int)ms.Position,
                  IsReliable = false
               });
            workSignal.Set();
         }
      }

      private async Task SendReliableMultipartPacketAsync(Guid destination, PacketDto packet) {
         using (var ms = new MemoryStream()) {
            await AsyncSerialize.ToAsync(ms, packet).ConfigureAwait(false);

            var payloadSize = ms.Position;

            // message id necessary for reassembly of chunks
            var multiPartMessageId = Guid.NewGuid();

            var chunkCount = (int)((payloadSize - 1) / UdpConstants.kMultiPartChunkSize + 1);
            var chunks = Arrays.Create(
               chunkCount,
               chunkIndex => {
                  var startIndexInclusive = UdpConstants.kMultiPartChunkSize * chunkIndex;
                  var endIndexExclusive = Math.Min(payloadSize, startIndexInclusive + UdpConstants.kMultiPartChunkSize);
                  return new MultiPartChunkDto {
                     Body = ms.GetBuffer(),
                     BodyLength = (int)(endIndexExclusive - startIndexInclusive),
                     BodyOffset = startIndexInclusive,
                     MultiPartMessageId = multiPartMessageId,
                     ChunkCount = chunkCount,
                     ChunkIndex = chunkIndex
                  };
               });
            logger.Info($"Splitting large payload into {chunks.Length} chunks.");

            await Task.WhenAll(
               Arrays.Create(
                  chunkCount,
                  i => SendReliableAsync(destination, MessageDto.Create(identity.Id, destination, chunks[i]))
                  )).ConfigureAwait(false);

            //            const int kConcurrencyLimit = 32;
            ////            var sync = new AsyncSemaphore(kConcurrencyLimit);
            //            for (var i = 0; i < chunkCount; i++) {
            ////               await sync.WaitAsync().ConfigureAwait(false);
            //               var chunkMessage = MessageDto.Create(identity.Id, destination, chunks[i]);
            ////               Go(async () => {
            //               SendReliableAsync(destination, chunkMessage).Forget();
            //.ConfigureAwait(false);
            //                  sync.Release();
            //               }).Forget();
         }
      }

      public class SendRequest {
         public IObjectPool<MemoryStream> DataBufferPool { get; set; }
         public MemoryStream Data { get; set; }
         public int DataOffset { get; set; }
         public int DataLength { get; set; }
         public bool IsReliable { get; set; }
         public AcknowledgementCoordinator.Signal AcknowledgementSignal { get; set; }
         public Guid AcknowledgementGuid { get; set; }
         public DateTime NextSendTime { get; set; } = DateTime.Now;
         public int SendCount { get; set; }
         public AsyncLatch CompletionSignal { get; set; }
      }
   }

   public class UdpTransport : ITransport {
      private readonly UdpBroadcaster udpBroadcaster;
      private readonly UdpFacade udpFacade;

      public UdpTransport(UdpBroadcaster udpBroadcaster, UdpFacade udpFacade) {
         this.udpBroadcaster = udpBroadcaster;
         this.udpFacade = udpFacade;
      }

      public Task SendMessageBroadcastAsync(MessageDto message) {
         return udpBroadcaster.SendBroadcastAsync(message);
      }

      public Task ShutdownAsync() {
         return udpFacade.ShutdownAsync();
      }
   }
}