using Dargon.Commons;
using Dargon.Commons.AsyncPrimitives;
using Dargon.Commons.Collections;
using Dargon.Commons.Pooling;
using Dargon.Courier.AuditingTier;
using Dargon.Courier.TransportTier.Udp.Vox;
using Dargon.Courier.Vox;
using Dargon.Vox;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons.Exceptions;
using static Dargon.Commons.Channels.ChannelsExtensions;

namespace Dargon.Courier.TransportTier.Udp {
   public class UdpBroadcaster {
      private readonly IObjectPool<MemoryStream> broadcastOutboundMemoryStreamPool = ObjectPool.CreateStackBacked(() => new MemoryStream(new byte[UdpConstants.kMaximumTransportSize], 0, UdpConstants.kMaximumTransportSize, true, true));
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
         for (var i = 0; i < Math.Max(4, Environment.ProcessorCount); i++) {
            new Thread(ProcessorThreadStart) { IsBackground = true }.Start();
         }
      }

      private static readonly ConcurrentQueue<Tuple<MemoryStream, object, TaskCompletionSource<byte>>> requestQueue = new ConcurrentQueue<Tuple<MemoryStream, object, TaskCompletionSource<byte>>>();
      private static readonly Semaphore requestSemaphore = new Semaphore(0, int.MaxValue);

      private static void ProcessorThreadStart() {
         while (true) {
            requestSemaphore.WaitOne();
            Tuple<MemoryStream, object, TaskCompletionSource<byte>> r;
            if (!requestQueue.TryDequeue(out r)) {
               throw new InvalidStateException();
            }
            try {
               Serialize.To(r.Item1, r.Item2);
               r.Item3.SetResult(0);
            } catch (Exception e) {
               r.Item3.SetException(e);
            }
         }
      }

      public static Task ToAsync(MemoryStream ms, object x) {
         var tcs = new TaskCompletionSource<byte>(TaskCreationOptions.RunContinuationsAsynchronously);
         requestQueue.Enqueue(Tuple.Create(ms, x, tcs));
         requestSemaphore.Release();
         return tcs.Task;
      }
   }

   public class UdpUnicaster {
      private readonly IObjectPool<MemoryStream> outboundMemoryStreamPool = ObjectPool.CreateStackBacked(() => new MemoryStream(new byte[UdpConstants.kMaximumTransportSize], 0, UdpConstants.kMaximumTransportSize, true, true));
      private readonly AsyncAutoResetLatch workSignal = new AsyncAutoResetLatch();
      private readonly Identity identity;
      private readonly UdpClient udpClient;
      private readonly AcknowledgementCoordinator acknowledgementCoordinator;
      private readonly ConcurrentQueue<SendRequest> unhandledSendRequestQueue = new ConcurrentQueue<SendRequest>();
      private readonly AuditCounter resendsCounter;
      private readonly AuditAggregator<int> resendsAggregator;
      private readonly AuditAggregator<double> outboundMessageRateLimitAggregator;
      private readonly AuditAggregator<double> sendQueueDepthAggregator;

      public UdpUnicaster(Identity identity, UdpClient udpClient, AcknowledgementCoordinator acknowledgementCoordinator, AuditCounter resendsCounter, AuditAggregator<int> resendsAggregator, AuditAggregator<double> outboundMessageRateLimitAggregator, AuditAggregator<double> sendQueueDepthAggregator) {
         this.identity = identity;
         this.udpClient = udpClient;
         this.acknowledgementCoordinator = acknowledgementCoordinator;
         this.resendsCounter = resendsCounter;
         this.resendsAggregator = resendsAggregator;
         this.outboundMessageRateLimitAggregator = outboundMessageRateLimitAggregator;
         this.sendQueueDepthAggregator = sendQueueDepthAggregator;
      }

      public void Initialize() {
         RunAsync().Forget();
      }

      private async Task RunAsync() {
         const int messageRateMin = 10000;
         const int deltaMessageRatePerSecond = 1000;
         var messageRateBase = messageRateMin * 2;
         var lastResetTime = DateTime.Now;

         Go(async () => {
            while (true) {
               workSignal.Set();
               await Task.Delay(100).ConfigureAwait(false);
            }
         }).Forget();

         var lastIterationEndTime = DateTime.Now;
         var inTransportSendRequestQueue = new ConcurrentQueue<SendRequest>();
         while (true) {
            await workSignal.WaitAsync().ConfigureAwait(false);
            sendQueueDepthAggregator.Put(unhandledSendRequestQueue.Count);

            var iterationStartTime = DateTime.Now;
            var messageRate = (int)(messageRateBase + (iterationStartTime - lastResetTime).TotalSeconds * deltaMessageRatePerSecond);
            outboundMessageRateLimitAggregator.Put(messageRate);

            var dt = iterationStartTime - lastIterationEndTime;
            var sendsRemaining = (int)Math.Floor(dt.TotalSeconds * messageRate);

            var pendingRenqueues = new List<SendRequest>();
            SendRequest noncapturedSendRequest;
            bool resendOccurred = false;

            while (sendsRemaining > 0 &&
                   (inTransportSendRequestQueue.TryDequeue(out noncapturedSendRequest) ||
                    unhandledSendRequestQueue.TryDequeue(out noncapturedSendRequest))) {
               var sendRequest = noncapturedSendRequest;
               if (!sendRequest.IsReliable) {
                  sendsRemaining--;
                  udpClient.Broadcast(
                     sendRequest.Data, sendRequest.DataOffset, sendRequest.DataLength,
                     () => {
                        Interlocked.Increment(ref DebugRuntimeStats.out_sent);
                        sendRequest.Data.SetLength(0);
                        outboundMemoryStreamPool.ReturnObject(sendRequest.Data);
                     });
               } else {
                  if (sendRequest.AcknowledgementSignal.IsSet()) {
                     resendsAggregator.Put(sendRequest.SendCount - 1);
                     sendRequest.Data.SetLength(0);
                     outboundMemoryStreamPool.ReturnObject(sendRequest.Data);
                     sendRequest.CompletionSignal.Set();
                     Interlocked.Increment(ref DebugRuntimeStats.out_rs_done);
                     continue;
                  }
                  if (DateTime.Now < sendRequest.NextSendTime) {
                     pendingRenqueues.Add(sendRequest);
                  } else {
                     sendsRemaining--;
                     if (sendRequest.SendCount > 0) {
                        resendsCounter.Increment();
                        resendOccurred = true;
                     }

                     Interlocked.Increment(ref DebugRuntimeStats.out_sent);
                     udpClient.Broadcast(
                        sendRequest.Data, sendRequest.DataOffset, sendRequest.DataLength,
                        () => {
                           const int kResendIntervalBase = 500;
                           var resendDelayMillis = kResendIntervalBase * Math.Min(32, 1 << sendRequest.SendCount);
                           sendRequest.NextSendTime = DateTime.Now + TimeSpan.FromMilliseconds(resendDelayMillis);
                           sendRequest.SendCount++;
                           inTransportSendRequestQueue.Enqueue(sendRequest);
                        });
                  }
               }
            }
            pendingRenqueues.ForEach(inTransportSendRequestQueue.Enqueue);
            if (resendOccurred) {
               messageRateBase = Math.Max(messageRateMin, messageRateBase + (messageRate - messageRateBase) / 2);
               lastResetTime = DateTime.Now;
            }
            lastIterationEndTime = DateTime.Now;
         }
      }

      public Task SendReliableAsync(Guid destination, MessageDto message) {
         Interlocked.Increment(ref DebugRuntimeStats.out_rs);
         return SendHelperAsync(destination, message, true);
         //         Interlocked.Increment(ref DebugRuntimeStats.out_rs_done);
      }

      public async Task SendUnreliableAsync(Guid destination, MessageDto message) {
         Interlocked.Increment(ref DebugRuntimeStats.out_nrs);
         await SendHelperAsync(destination, message, false).ConfigureAwait(false);
         Interlocked.Increment(ref DebugRuntimeStats.out_nrs_done);
      }

      private async Task SendHelperAsync(Guid destination, MessageDto message, bool reliable) {
         var packetDto = PacketDto.Create(identity.Id, destination, message, reliable);
         var ms = outboundMemoryStreamPool.TakeObject();
         ms.SetLength(0);
         try {
            await AsyncSerialize.ToAsync(ms, packetDto).ConfigureAwait(false);
         } catch (NotSupportedException) {
            // surpassed memory stream buffer size.
            if (!reliable) {
               throw new InvalidOperationException("Attempted to send nonreliable buffer surpassing transport buffer size");
            }

            ms.SetLength(0);
            outboundMemoryStreamPool.ReturnObject(ms);
            await SendReliableMultipartPacketAsync(destination, packetDto).ConfigureAwait(false);
            return;
         }

         if (reliable) {
            var completionSignal = new AsyncLatch();
            var acknowlewdgementSignal = acknowledgementCoordinator.Expect(packetDto.Id);
            unhandledSendRequestQueue.Enqueue(
               new SendRequest {
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
                  Data = ms,
                  DataOffset = 0,
                  DataLength = (int)ms.Position,
                  IsReliable = false
               });
            workSignal.Set();
            await Task.FromResult(false).ConfigureAwait(false);
         }
      }

      private async Task SendReliableMultipartPacketAsync(Guid destination, PacketDto packet) {
         using (var ms = new MemoryStream()) {
            await AsyncSerialize.ToAsync(ms, packet).ConfigureAwait(false);

            var payloadSize = ms.Position;

            // message id necessary for reassembly of chunks
            var multiPartMessageId = Guid.NewGuid();

            var chunkCount = (int)((payloadSize - 1) / UdpConstants.kMultiPartChunkSize + 1);
            var chunks = Util.Generate(
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

            await Task.WhenAll(
               Util.Generate(
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