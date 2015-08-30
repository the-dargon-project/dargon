using System;

namespace Dargon.Courier.Messaging {
   public interface MessageRouter {
      void RegisterPayloadHandler<T>(Action<IReceivedMessage<T>> handler);
      void RegisterPayloadHandler(Type t, Action<IReceivedMessage<object>> handler);

      void RouteMessage<TPayload>(IReceivedMessage<TPayload> receivedMessage);
      void RouteMessage(Type payloadType, IReceivedMessage<object> receivedMessage);
   }
}
