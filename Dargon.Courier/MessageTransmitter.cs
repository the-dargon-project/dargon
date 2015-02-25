using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dargon.Courier {
   public interface MessageTransmitter {
      // Guid SendReliableUnicast<TMessage>(CourierEndpoint endpoint, TMessage message, MessagePriority priority);
      // Guid SendUnreliableUnicast<TMessage>(CourierEndpoint endpoint, TMessage message);
      // IReadOnlyDictionary<CourierEndpoint, Guid> SendReliableMulticast<TMessage>(IReadOnlyList<CourierEndpoint> endpoints, TMessage message, MessagePriority priority);
      // IReadOnlyDictionary<CourierEndpoint, Guid> SendUnreliableMulticast<TMessage>(IReadOnlyList<CourierEndpoint> endpoints, TMessage message);
      // Guid SendBroadcast<TMessage>(TMessage message);
      // void SetMessageCompletion(Guid messageId);
   }
}
