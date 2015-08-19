using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Dargon.Courier.Messaging {
   public class ReceivedMessage<TPayload> : IReceivedMessage<TPayload> {
      private readonly Guid guid;
      private readonly Guid senderId;
      private readonly Guid recipientId;
      private readonly MessageFlags messageFlags;
      private readonly TPayload payload;
      private readonly IPAddress remoteAddress;

      public ReceivedMessage(Guid guid, Guid senderId, Guid recipientId, MessageFlags messageFlags, TPayload payload, IPAddress remoteAddress) {
         this.guid = guid;
         this.senderId = senderId;
         this.recipientId = recipientId;
         this.messageFlags = messageFlags;
         this.payload = payload;
         this.remoteAddress = remoteAddress;
      }

      public Guid Guid { get { return guid; } }
      public Guid SenderId { get { return senderId; } }
      public Guid RecipientId { get { return recipientId; } }
      public MessageFlags MessageFlags { get { return messageFlags; } }
      public TPayload Payload { get { return payload; } }
      public IPAddress RemoteAddress => remoteAddress;
   }
}
