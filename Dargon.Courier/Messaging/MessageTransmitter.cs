using System;

namespace Dargon.Courier.Messaging {
   public interface MessageTransmitter {
      // Guid SendReliableUnicast<TMessage>(CourierEndpoint endpoint, TMessage message, MessagePriority priority);
      // Guid SendUnreliableUnicast<TMessage>(CourierEndpoint endpoint, TMessage message);
      // IReadOnlyDictionary<CourierEndpoint, Guid> SendReliableMulticast<TMessage>(IReadOnlyList<CourierEndpoint> endpoints, TMessage message, MessagePriority priority);
      // IReadOnlyDictionary<CourierEndpoint, Guid> SendUnreliableMulticast<TMessage>(IReadOnlyList<CourierEndpoint> endpoints, TMessage message);
      Guid SendBroadcast<TMessage>(TMessage message);
      // void SetMessageCompletion(Guid messageId);
   }
}
