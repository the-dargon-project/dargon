using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NMockito.Attributes;
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

   public class MessageDispatcherTest : NMockitoInstance {
      [Mock] private readonly EventBus<Message> messageBus = null;
      [Mock] private readonly PingService pingService = null;
      [Mock] private readonly PeerDiscoveryService peerDiscoveryService = null;

      private readonly MessageDispatcher testObj;

      public MessageDispatcherTest() {
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
            m.Size == MessageDispatcher.kMessageSizeLimit + 1 && 
            m.Type == MessageType.Unknown &&
            m.Type == (MessageType)10);

         Console.WriteLine(message.Size);
         Console.WriteLine(message.Type);
         testObj.HandleMessage(messageBus, message);

         VerifyExpectationsAndNoMoreInteractions();
      }
   }
}
