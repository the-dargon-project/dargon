using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Courier.PortableObjects;
using Dargon.PortableObjects;
using ItzWarty.Collections;

namespace Dargon.Courier.Messaging {
   public interface MessageRouter {
      void RegisterPayloadHandler<T>(Action<IReceivedMessage<T>> handler);
      void RegisterPayloadHandler(Type t, Action<IReceivedMessage<object>> handler);

      void RouteMessage(Guid senderId, CourierMessageV1 message);
      void RouteMessage<TPayload>(IReceivedMessage<TPayload> receivedMessage);
      void RouteMessage(Type payloadType, IReceivedMessage<object> receivedMessage);
   }

   public class MessageRouterImpl : MessageRouter {
      private readonly IConcurrentDictionary<Type, Action<IReceivedMessage<object>>> handlersByPayloadType;
      private readonly ReceivedMessageFactory receivedMessageFactory;

      public MessageRouterImpl(ReceivedMessageFactory receivedMessageFactory)
         : this(receivedMessageFactory, new ConcurrentDictionary<Type, Action<IReceivedMessage<object>>>()) {

      }

      public MessageRouterImpl(ReceivedMessageFactory receivedMessageFactory, IConcurrentDictionary<Type, Action<IReceivedMessage<object>>> handlersByPayloadType) {
         this.receivedMessageFactory = receivedMessageFactory;
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

      public void RouteMessage(Guid senderId, CourierMessageV1 message) {
         var receivedMessage = receivedMessageFactory.CreateReceivedMessage(senderId, message);
         var payloadType = receivedMessage.Payload.GetType();

         RouteMessage(payloadType, receivedMessage);
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
