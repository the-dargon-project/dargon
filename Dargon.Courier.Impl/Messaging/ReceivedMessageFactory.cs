using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Dargon.Courier.PortableObjects;
using Dargon.PortableObjects;
using ItzWarty.Collections;

namespace Dargon.Courier.Messaging {
   public interface ReceivedMessageFactory {
      IReceivedMessage<object> CreateReceivedMessage(Guid senderId, CourierMessageV1 message);
   }

   public class ReceivedMessageFactoryImpl : ReceivedMessageFactory {
      private readonly IPofSerializer pofSerializer;
      private readonly IConcurrentDictionary<Type, MethodInfo> receivedMessageFactoriesByPayloadType;

      public ReceivedMessageFactoryImpl(IPofSerializer pofSerializer) {
         this.pofSerializer = pofSerializer;
         this.receivedMessageFactoriesByPayloadType = new ConcurrentDictionary<Type, MethodInfo>();
      }

      public IReceivedMessage<object> CreateReceivedMessage(Guid senderId, CourierMessageV1 message) {
         var payload = pofSerializer.Deserialize(new MemoryStream(message.Payload));
         var payloadType = payload.GetType();
         var receivedMessageFactory = receivedMessageFactoriesByPayloadType.GetOrAdd(
            payloadType,
            add => GetType().GetMethod(nameof(CreateReceivedMessageHelper), BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(payloadType)
         );
         return (IReceivedMessage<object>)receivedMessageFactory.Invoke(this, new[] { senderId, message, payload });
      } 

      private IReceivedMessage<object> CreateReceivedMessageHelper<TPayload>(Guid senderId, CourierMessageV1 message, TPayload payload) {
         return (IReceivedMessage<object>)new ReceivedMessage<TPayload>(
            message.Id,
            senderId,
            message.RecipientId,
            message.MessageFlags,
            payload
         );
      }
   }
}
