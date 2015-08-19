using System;
using System.Net;

namespace Dargon.Courier.Messaging {
   public interface IReceivedMessage<out TPayload> {
      Guid Guid { get; }
      Guid SenderId { get; }
      Guid RecipientId { get; }
      MessageFlags MessageFlags { get; }
      TPayload Payload { get; }
      IPAddress RemoteAddress { get; }
   }
}
