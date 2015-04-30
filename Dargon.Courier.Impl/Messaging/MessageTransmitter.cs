using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Courier.Identities;
using Dargon.Courier.PortableObjects;
using Dargon.PortableObjects;
using ItzWarty;
using ItzWarty.Networking;
using ItzWarty.Pooling;

namespace Dargon.Courier.Messaging {
   public interface MessageTransmitter {
      void Transmit<TPayload>(Guid messageId, Guid recipientId, TPayload payload, MessageFlags messageFlags);
      //Guid SendReliableUnicast<TMessage>(ReadableCourierEndpoint endpoint, TMessage message, MessagePriority priority);
      // Guid SendUnreliableUnicast<TMessage>(CourierEndpoint endpoint, TMessage message);
      // IReadOnlyDictionary<CourierEndpoint, Guid> SendReliableMulticast<TMessage>(IReadOnlyList<CourierEndpoint> endpoints, TMessage message, MessagePriority priority);
      // IReadOnlyDictionary<CourierEndpoint, Guid> SendUnreliableMulticast<TMessage>(IReadOnlyList<CourierEndpoint> endpoints, TMessage message);
      //Guid SendBroadcast<TMessage>(TMessage payload);
      // void SetMessageCompletion(Guid messageId);
   }

   public class MessageTransmitterImpl : MessageTransmitter {
      private readonly IPofSerializer pofSerializer;
      private readonly NetworkBroadcaster networkBroadcaster;
      private readonly GuidProxy guidProxy;
      private readonly UnacknowledgedReliableMessageContainer unacknowledgedReliableMessageContainer;
      private readonly ObjectPool<CourierMessageV1> messageDtoPool;

      public MessageTransmitterImpl(GuidProxy guidProxy, IPofSerializer pofSerializer, NetworkBroadcaster networkBroadcaster, UnacknowledgedReliableMessageContainer unacknowledgedReliableMessageContainer, ObjectPool<CourierMessageV1> messageDtoPool) {
         this.guidProxy = guidProxy;
         this.pofSerializer = pofSerializer;
         this.networkBroadcaster = networkBroadcaster;
         this.unacknowledgedReliableMessageContainer = unacknowledgedReliableMessageContainer;
         this.messageDtoPool = messageDtoPool;
      }

//      public Guid SendReliableUnicast<TMessage>(ReadableCourierEndpoint endpoint, TMessage message, MessagePriority priority) {
//         var messageId = guidProxy.NewGuid();
//         SendInternal(endpoint.Identifier, message, MessageFlags.AcknowledgementRequired);
//         unacknowledgedReliableMessageContainer.AddMessage(endpoint.Identifier, message, priority, messageId);
//      }
//
//      public Guid SendBroadcast<TPayload>(TPayload payload) {
//         return SendInternal(Guid.Empty, payload, MessageFlags.Default);
//      }


      public void Transmit<TPayload>(Guid messageId, Guid recipientId, TPayload payload, MessageFlags messageFlags) {
         using (var ms = new MemoryStream())
         using (var writer = new BinaryWriter(ms)) {
            pofSerializer.Serialize(writer, (object)payload);

            var messageDto = messageDtoPool.TakeObject();
            messageDto.Update(messageId, recipientId, messageFlags, ms.GetBuffer(), 0, (int)ms.Length);

            networkBroadcaster.SendCourierPacket(messageDto);

            messageDtoPool.ReturnObject(messageDto);
         }
      }
   }
}
