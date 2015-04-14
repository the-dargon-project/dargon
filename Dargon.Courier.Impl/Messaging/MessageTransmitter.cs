using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Courier.PortableObjects;
using Dargon.PortableObjects;
using ItzWarty;
using ItzWarty.Networking;

namespace Dargon.Courier.Messaging {
   public interface MessageTransmitter {
      // Guid SendReliableUnicast<TMessage>(CourierEndpoint endpoint, TMessage message, MessagePriority priority);
      // Guid SendUnreliableUnicast<TMessage>(CourierEndpoint endpoint, TMessage message);
      // IReadOnlyDictionary<CourierEndpoint, Guid> SendReliableMulticast<TMessage>(IReadOnlyList<CourierEndpoint> endpoints, TMessage message, MessagePriority priority);
      // IReadOnlyDictionary<CourierEndpoint, Guid> SendUnreliableMulticast<TMessage>(IReadOnlyList<CourierEndpoint> endpoints, TMessage message);
      Guid SendBroadcast<TMessage>(TMessage payload);
      // void SetMessageCompletion(Guid messageId);
   }

   public class MessageTransmitterImpl : MessageTransmitter {
      private readonly IPofSerializer pofSerializer;
      private readonly NetworkBroadcaster networkBroadcaster;
      private readonly GuidProxy guidProxy;

      public MessageTransmitterImpl(GuidProxy guidProxy, IPofSerializer pofSerializer, NetworkBroadcaster networkBroadcaster) {
         this.guidProxy = guidProxy;
         this.pofSerializer = pofSerializer;
         this.networkBroadcaster = networkBroadcaster;
      }

      public Guid SendBroadcast<TPayload>(TPayload payload) {
         using (var ms = new MemoryStream())
         using (var writer = new BinaryWriter(ms)) {
            pofSerializer.Serialize(writer, (object)payload);
            var messageId = guidProxy.NewGuid();
            networkBroadcaster.SendCourierPacket(
               new CourierMessageV1(
                  messageId,
                  Guid.Empty,
                  MessageFlags.Default,
                  ms.GetBuffer(),
                  0,
                  (int)ms.Length
               )
            );
            return messageId;
         }
      }
   }
}
