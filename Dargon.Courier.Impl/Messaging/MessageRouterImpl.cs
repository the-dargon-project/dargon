using System;
using System.Net;
using Dargon.Courier.PortableObjects;
using ItzWarty.Collections;

namespace Dargon.Courier.Messaging {
   public class MessageRouterImpl : MessageRouter {
      private readonly IConcurrentDictionary<Type, Action<IReceivedMessage<object>>> handlersByPayloadType;

      public MessageRouterImpl() : this(new ConcurrentDictionary<Type, Action<IReceivedMessage<object>>>()) { }

      public MessageRouterImpl(IConcurrentDictionary<Type, Action<IReceivedMessage<object>>> handlersByPayloadType) {
         this.handlersByPayloadType = handlersByPayloadType;
      }

      public void RegisterPayloadHandler<T>(Action<IReceivedMessage<T>> handler) {
         RegisterPayloadHandler(typeof(T), x => handler((IReceivedMessage<T>)x));
      }
      
      public void RegisterPayloadHandler(Type t, Action<IReceivedMessage<object>> handler) {
         handlersByPayloadType.AddOrUpdate(
            t,
            value => handler,
            (key, existing) => { throw new InvalidOperationException("Handler for " + t.FullName + " already exists!"); }
            );
      }

      public void RouteMessage<TPayload>(IReceivedMessage<TPayload> receivedMessage) {
         RouteMessage(typeof(TPayload), (IReceivedMessage<object>)receivedMessage);
      }

      public void RouteMessage(Type payloadType, IReceivedMessage<object> receivedMessage) {
         Action<IReceivedMessage<object>> handler;
         if (handlersByPayloadType.TryGetValue(payloadType, out handler)) {
            handler(receivedMessage);
         }
      }
   }
}