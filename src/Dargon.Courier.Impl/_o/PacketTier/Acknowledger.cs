using Dargon.Commons.Pooling;
using Dargon.Courier.Vox;
using System.Threading.Tasks;

namespace Dargon.Courier.PacketTier {
   public class Acknowledger {
      private readonly IObjectPool<AcknowledgementDto> acknowledgementDtoPool = ObjectPool.Create(() => new AcknowledgementDto());
      private readonly OutboundPayloadEventEmitter outboundPayloadEventEmitter;

      public Acknowledger(OutboundPayloadEventEmitter outboundPayloadEventEmitter) {
         this.outboundPayloadEventEmitter = outboundPayloadEventEmitter;
      }

      public async Task AcknowledgeAsync(InboundPacketEvent e) {
         var ack = acknowledgementDtoPool.TakeObject();
         ack.MessageId = e.Packet.Id;
         await outboundPayloadEventEmitter.EmitAsync(ack, e);
         acknowledgementDtoPool.ReturnObject(ack);
      }
   }
}