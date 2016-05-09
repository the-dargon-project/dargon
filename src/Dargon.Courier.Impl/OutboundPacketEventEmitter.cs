using System;
using System.Threading;
using Dargon.Commons.Pooling;
using Dargon.Courier.Vox;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Courier.PacketTier;

namespace Dargon.Courier {
   public class OutboundPacketEventEmitter {
      private readonly IObjectPool<OutboundPacketEvent> outboundPacketEventPool = ObjectPool.Create(() => new OutboundPacketEvent { Packet = new PacketDto() });
      private readonly IAsyncPoster<OutboundPacketEvent> outboundPacketEventPoster;
      private readonly AcknowledgementCoordinator acknowledgementCoordinator;

      public OutboundPacketEventEmitter(IAsyncPoster<OutboundPacketEvent> outboundPacketEventPoster, AcknowledgementCoordinator acknowledgementCoordinator) {
         this.outboundPacketEventPoster = outboundPacketEventPoster;
         this.acknowledgementCoordinator = acknowledgementCoordinator;
      }

      public async Task EmitAsync(object payload, Guid destination, bool reliable, object tagEvent) {
         await Task.Yield();

         var outboundPacketEvent = outboundPacketEventPool.TakeObject();
         outboundPacketEvent.Packet.Id = Guid.NewGuid();
         outboundPacketEvent.Packet.Payload = payload;
         outboundPacketEvent.Packet.Destination = destination;
         outboundPacketEvent.Packet.Flags = reliable ? PacketFlags.Reliable : PacketFlags.None;
         outboundPacketEvent.TagEvent = tagEvent;

         if (!reliable) {
            await outboundPacketEventPoster.PostAsync(outboundPacketEvent);
         } else {
            using (var cts = new CancellationTokenSource()) {
               var expectation = acknowledgementCoordinator.Expect(
                  outboundPacketEvent.Packet.Id,
                  cts.Token);
               expectation.ContinueWith(state => {
                  cts.Cancel();
               }, cts.Token).Forget();

               const int resendDelay = 1000;
               while (!expectation.IsCompleted) {
                  try {
                     await outboundPacketEventPoster.PostAsync(outboundPacketEvent);
                     await Task.Delay(resendDelay, cts.Token);
                  } catch (TaskCanceledException) {
                     // It's on the Task.Delay
                  }
               }
            }
         }

         outboundPacketEvent.Packet.Id = Guid.Empty;
         outboundPacketEvent.Packet.Payload = null;
         outboundPacketEvent.Packet.Destination = Guid.Empty;
         outboundPacketEvent.Packet.Flags = 0;
         outboundPacketEventPool.ReturnObject(outboundPacketEvent);
      }
   }
}