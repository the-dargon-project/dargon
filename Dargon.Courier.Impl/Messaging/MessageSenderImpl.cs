using System;
using ItzWarty;

namespace Dargon.Courier.Messaging {
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