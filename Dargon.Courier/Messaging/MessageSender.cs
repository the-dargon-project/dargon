using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Courier.Identities;

namespace Dargon.Courier.Messaging {
   public interface MessageSender {
      void SendReliableUnicast<TMessage>(Guid recipientId, TMessage payload, MessagePriority priority);
      void SendUnreliableUnicast<TMessage>(Guid recipientId, TMessage message);
      // IReadOnlyDictionary<CourierEndpoint, Guid> SendReliableMulticast<TMessage>(IReadOnlyList<CourierEndpoint> endpoints, TMessage message, MessagePriority priority);
      // IReadOnlyDictionary<CourierEndpoint, Guid> SendUnreliableMulticast<TMessage>(IReadOnlyList<CourierEndpoint> endpoints, TMessage message);
      void SendBroadcast<TMessage>(TMessage payload);
      // void SetMessageCompletion(Guid messageId);
   }
}
