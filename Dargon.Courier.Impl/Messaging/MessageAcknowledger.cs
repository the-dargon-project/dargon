using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Courier.PortableObjects;
using ItzWarty.Pooling;

namespace Dargon.Courier.Messaging {
   public interface MessageAcknowledger {
      void SendAcknowledge(Guid senderId, Guid messageId);
      void HandleAcknowledge(Guid senderId, Guid messageId);
   }

   public class MessageAcknowledgerImpl : MessageAcknowledger {
      private readonly NetworkBroadcaster networkBroadcaster;
      private readonly UnacknowledgedReliableMessageContainer unacknowledgedReliableMessageContainer;
      private readonly ObjectPool<CourierMessageAcknowledgeV1> acknowledgeDtoPool;

      public MessageAcknowledgerImpl(NetworkBroadcaster networkBroadcaster, UnacknowledgedReliableMessageContainer unacknowledgedReliableMessageContainer, ObjectPool<CourierMessageAcknowledgeV1> acknowledgeDtoPool) {
         this.networkBroadcaster = networkBroadcaster;
         this.unacknowledgedReliableMessageContainer = unacknowledgedReliableMessageContainer;
         this.acknowledgeDtoPool = acknowledgeDtoPool;
      }

      public void SendAcknowledge(Guid senderId, Guid messageId) {
         var packet = acknowledgeDtoPool.TakeObject();
         packet.Update(
            recipientId: senderId,
            messageId: messageId
         );
         networkBroadcaster.SendCourierPacket(packet);
         acknowledgeDtoPool.ReturnObject(packet);
      }

      public void HandleAcknowledge(Guid senderId, Guid messageId) {
         unacknowledgedReliableMessageContainer.HandleMessageAcknowledged(senderId, messageId);
      }
   }
}
