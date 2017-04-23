using NMockito.Attributes;
using System;
using NMockito.Fluent;
using Xunit;

// ReSharper disable TestClassNameDoesNotMatchFileNameWarning

namespace NMockito {
   public interface IEventBus<T> {
      void Post(T message);
      event EventHandler<T> Receive;
   }

   public enum MessageType {
      Unknown,
      Announce,
      Ping
   }

   public interface IMessage {
      int Size { get; }
      MessageType Type { get; }
   }
   
   public interface IPingService {
      void HandlePing(IMessage message);
   }

   public interface IPeerDiscoveryService {
      void HandleAnnounce(IMessage message);
   }

   public class MessageDispatcher {
      internal const int kMessageSizeLimit = 1024;

      private readonly IEventBus<IMessage> messageBus;
      private readonly IPingService pingService;
      private readonly IPeerDiscoveryService peerDiscoveryService;

      public MessageDispatcher(IEventBus<IMessage> messageBus, IPingService pingService, IPeerDiscoveryService peerDiscoveryService) {
         this.messageBus = messageBus;
         this.pingService = pingService;
         this.peerDiscoveryService = peerDiscoveryService;
      }

      public void Initialize() {
         messageBus.Receive += HandleMessage;
      }

      internal void HandleMessage(object sender, IMessage message) {
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
      [Mock] private readonly IEventBus<IMessage> messageBus = null;
      [Mock] private readonly IPingService pingService = null;
      [Mock] private readonly IPeerDiscoveryService peerDiscoveryService = null;

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
         var message = CreateMock<IMessage>(m =>
            m.Size == MessageDispatcher.kMessageSizeLimit + 1);

         testObj.HandleMessage(messageBus, message);

         VerifyExpectationsAndNoMoreInteractions();
      }

      [Fact]
      public void HandleMessage_WithPing_DelegatesToPingService() {
         var message = CreateMock<IMessage>(m =>
            m.Size == MessageDispatcher.kMessageSizeLimit &&
            m.Type == MessageType.Ping);

         Expect(() => pingService.HandlePing(message)).ThenReturn();

         testObj.HandleMessage(messageBus, message);

         VerifyExpectationsAndNoMoreInteractions();
      }

      [Fact]
      public void HandleMessage_WithAnnounce_DelegatesToPeerDiscoveryService() {
         var message = CreateMock<IMessage>(m =>
            m.Size == MessageDispatcher.kMessageSizeLimit &&
            m.Type == MessageType.Announce);

         Expect(() => peerDiscoveryService.HandleAnnounce(message)).ThenReturn();

         testObj.HandleMessage(messageBus, message);

         VerifyExpectationsAndNoMoreInteractions();
      }

      [Fact]
      public void HandleMessage_WithUnhandledMessage_Throws() {
         var message = CreateMock<IMessage>(m =>
            m.Size == MessageDispatcher.kMessageSizeLimit &&
            m.Type == MessageType.Unknown);

         Assert(() => testObj.HandleMessage(messageBus, message)).Throws<NotSupportedException>();

         VerifyExpectationsAndNoMoreInteractions();
      }
   }
}
