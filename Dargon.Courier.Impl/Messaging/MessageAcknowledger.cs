using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Courier.PortableObjects;

namespace Dargon.Courier.Messaging {
   public interface MessageAcknowledger {
      void SendAcknowledge(Guid senderId, Guid messageId);
      void HandleAcknowledge(Guid senderId, Guid messageId);
   }

   public class MessageAcknowledgerImpl : MessageAcknowledger {
      private readonly NetworkBroadcaster networkBroadcaster;
      private readonly UnacknowledgedReliableMessageContainer unacknowledgedReliableMessageContainer;

      public MessageAcknowledgerImpl(NetworkBroadcaster networkBroadcaster, UnacknowledgedReliableMessageContainer unacknowledgedReliableMessageContainer) {
         this.networkBroadcaster = networkBroadcaster;
         this.unacknowledgedReliableMessageContainer = unacknowledgedReliableMessageContainer;
      }

      public void SendAcknowledge(Guid senderId, Guid messageId) {
         networkBroadcaster.SendCourierPacket(
            new CourierMessageAcknowledgeV1(
               senderId,
               messageId
            )
         );
      }

      public void HandleAcknowledge(Guid senderId, Guid messageId) {
         unacknowledgedReliableMessageContainer.HandleMessageAcknowledged(senderId, messageId);
      }
   }
}
