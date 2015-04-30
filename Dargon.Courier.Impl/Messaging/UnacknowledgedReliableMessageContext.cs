using System;

namespace Dargon.Courier.Messaging {
   public class UnacknowledgedReliableMessageContext {
      public Guid RecipientId { get; private set; }
      public Guid MessageId { get; private set; }
      public object Payload { get; private set; }
      public MessagePriority Priority { get; private set; }
      public MessageFlags Flags { get; private set; }
      public bool Acknowledged { get; private set; }

      public void MarkAcknowledged() {
         Acknowledged = true;
      }

      public void UpdateAndMarkUnacknowledged(Guid messageId, Guid recipientId, object payload, MessagePriority priority, MessageFlags messageFlags) {
         this.RecipientId = recipientId;
         this.MessageId = messageId;
         this.Payload = payload;
         this.Priority = priority;
         this.Flags = messageFlags;
         this.Acknowledged = false;
      }
   }
}