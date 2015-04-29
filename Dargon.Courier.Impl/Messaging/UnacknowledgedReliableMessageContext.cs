using System;

namespace Dargon.Courier.Messaging {
   public class UnacknowledgedReliableMessageContext {
      private readonly Guid recipientId;
      private readonly Guid messageId;
      private readonly object payload;
      private readonly MessagePriority priority;
      private readonly MessageFlags messageFlags;
      private bool acknowledged = false;

      public UnacknowledgedReliableMessageContext(Guid messageId, Guid recipientId, object payload, MessagePriority priority, MessageFlags messageFlags) {
         this.recipientId = recipientId;
         this.messageId = messageId;
         this.payload = payload;
         this.priority = priority;
         this.messageFlags = messageFlags;
      }

      public Guid RecipientId {  get { return recipientId; } }
      public Guid MessageId { get { return messageId; } }
      public object Payload {  get { return payload; } }
      public MessagePriority Priority { get { return priority; } }
      public MessageFlags Flags { get { return messageFlags; } }
      public bool Acknowledged { get { return acknowledged; } }

      public void MarkAcknowledged() {
         acknowledged = true;
      }
   }
}