using NMockito.Attributes;
using System;
using NMockito.Fluent;
using Xunit;

namespace NMockito {
   public interface EventBus<T> {
      void Post(T message);
      event EventHandler<T> Receive;
   }

   public enum MessageType {
      Unknown,
      Announce,
      Ping
   }

   public interface Message {
      int Size { get; }
      MessageType Type { get; }
   }
   
   public interface PingService {
      void HandlePing(Message message);
   }

   public interface PeerDiscoveryService {
      void HandleAnnounce(Message message);
   }

   public class MessageDispatcher {
      internal const int kMessageSizeLimit = 1024;

      private readonly EventBus<Message> messageBus;
      private readonly PingService pingService;
      private readonly PeerDiscoveryService peerDiscoveryService;

      public MessageDispatcher(EventBus<Message> messageBus, PingService pingService, PeerDiscoveryService peerDiscoveryService) {
         this.messageBus = messageBus;
         this.pingService = pingService;
         this.peerDiscoveryService = peerDiscoveryService;
      }

      public void Initialize() {
         messageBus.Receive += HandleMessage;
      }

      internal void HandleMessage(object sender, Message message) {
         if (message.Size > kMessageSizeLimit) {
            return;
         }

         if (message.Type == MessageType.Ping) {
            pingService.HandlePing(message);
         } else if (message.Type == MessageType.Announce) {
            peerDiscoveryService.HandleAnnounce(message);
         } else {
            throw new NotSupportedException($"Unhandled message type: {message.Type}");
         }
      }
   }

   public class MessageDispatcherTests : NMockitoInstance {
      [Mock] private readonly EventBus<Message> messageBus = null;
      [Mock] private readonly PingService pingService = null;
      [Mock] private readonly PeerDiscoveryService peerDiscoveryService = null;

      private readonly MessageDispatcher testObj;

      public MessageDispatcherTests() {
         this.testObj = new MessageDispatcher(messageBus, pingService, peerDiscoveryService);
      }

      [Fact]
      public void Initialize_SubscribesToMessageBus() {
         Expect(() => messageBus.Receive += testObj.HandleMessage).ThenReturn();

         testObj.Initialize();

         VerifyExpectationsAndNoMoreInteractions();
      }

      [Fact]
      public void HandleMessage_WithSurpassedSizeLimit_DoesNothing() {
         var message = CreateMock<Message>(m =>
            m.Size == MessageDispatcher.kMessageSizeLimit + 1);

         testObj.HandleMessage(messageBus, message);

         VerifyExpectationsAndNoMoreInteractions();
      }

      [Fact]
      public void HandleMessage_WithPing_DelegatesToPingService() {
         var message = CreateMock<Message>(m =>
            m.Size == MessageDispatcher.kMessageSizeLimit &&
            m.Type == MessageType.Ping);

         Expect(() => pingService.HandlePing(message)).ThenReturn();

         testObj.HandleMessage(messageBus, message);

         VerifyExpectationsAndNoMoreInteractions();
      }

      [Fact]
      public void HandleMessage_WithAnnounce_DelegatesToPeerDiscoveryService() {
         var message = CreateMock<Message>(m =>
            m.Size == MessageDispatcher.kMessageSizeLimit &&
            m.Type == MessageType.Announce);

         Expect(() => peerDiscoveryService.HandleAnnounce(message)).ThenReturn();

         testObj.HandleMessage(messageBus, message);

         VerifyExpectationsAndNoMoreInteractions();
      }

      [Fact]
      public void HandleMessage_WithUnhandledMessage_Throws() {
         var message = CreateMock<Message>(m =>
            m.Size == MessageDispatcher.kMessageSizeLimit &&
            m.Type == MessageType.Unknown);

         Assert(() => testObj.HandleMessage(messageBus, message)).Throws<NotSupportedException>();

         VerifyExpectationsAndNoMoreInteractions();
      }
   }
}
