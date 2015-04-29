using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Courier.Identities;
using ItzWarty;

namespace Dargon.Courier.Messaging {
   public interface MessageSender {
      void SendReliableUnicast<TMessage>(Guid recipientId, TMessage payload, MessagePriority priority);
      void SendUnreliableUnicast<TMessage>(Guid recipientId, TMessage message);
      // IReadOnlyDictionary<CourierEndpoint, Guid> SendReliableMulticast<TMessage>(IReadOnlyList<CourierEndpoint> endpoints, TMessage message, MessagePriority priority);
      // IReadOnlyDictionary<CourierEndpoint, Guid> SendUnreliableMulticast<TMessage>(IReadOnlyList<CourierEndpoint> endpoints, TMessage message);
      void SendBroadcast<TMessage>(TMessage payload);
      // void SetMessageCompletion(Guid messageId);
   }

   public class MessageSenderImpl : MessageSender {
      private readonly GuidProxy guidProxy;
      private readonly UnacknowledgedReliableMessageContainer unacknowledgedReliableMessageContainer;
      private readonly MessageTransmitter messageTransmitter;

      public MessageSenderImpl(GuidProxy guidProxy, UnacknowledgedReliableMessageContainer unacknowledgedReliableMessageContainer, MessageTransmitter messageTransmitter) {
         this.guidProxy = guidProxy;
         this.unacknowledgedReliableMessageContainer = unacknowledgedReliableMessageContainer;
         this.messageTransmitter = messageTransmitter;
      }

      public void SendReliableUnicast<TMessage>(Guid recipientId, TMessage payload, MessagePriority priority) {
         var messageId = guidProxy.NewGuid();
         var messageFlags = MessageFlags.AcknowledgementRequired;
         unacknowledgedReliableMessageContainer.AddMessage(messageId, recipientId, payload, priority, messageFlags);
         messageTransmitter.Transmit(messageId, recipientId, payload, messageFlags);
      }

      public void SendUnreliableUnicast<TMessage>(Guid recipientId, TMessage payload) {
         var messageId = guidProxy.NewGuid();
         messageTransmitter.Transmit(messageId, recipientId, payload, MessageFlags.AcknowledgementRequired);
      }

      public void SendBroadcast<TMessage>(TMessage payload) {
         var messageId = guidProxy.NewGuid();
         var recipientId = Guid.Empty;
         messageTransmitter.Transmit(messageId, recipientId, payload, MessageFlags.AcknowledgementRequired);
      }
   }
}
