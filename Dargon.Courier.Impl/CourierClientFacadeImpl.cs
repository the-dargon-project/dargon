using System;
using System.Collections.Generic;
using System.Net;
using Dargon.Courier.Identities;
using Dargon.Courier.Messaging;
using Dargon.Courier.Peering;

namespace Dargon.Courier {
   public class CourierClientFacadeImpl : CourierClient {
      private readonly ManageableCourierEndpoint localEndpoint;
      private readonly MessageSender messageSender;
      private readonly MessageRouter messageRouter;
      private readonly ReadablePeerRegistry peerRegistry;

      public CourierClientFacadeImpl(ManageableCourierEndpoint localEndpoint, MessageSender messageSender, MessageRouter messageRouter, ReadablePeerRegistry peerRegistry) {
         this.localEndpoint = localEndpoint;
         this.messageSender = messageSender;
         this.messageRouter = messageRouter;
         this.peerRegistry = peerRegistry;
      }

      public ManageableCourierEndpoint LocalEndpoint => localEndpoint;
      public MessageSender MessageSender => messageSender;
      public MessageRouter MessageRouter => messageRouter;
      public ReadablePeerRegistry PeerRegistry => peerRegistry;

      #region ManageableCourierEndpoint
      public IPAddress InitialAddress => localEndpoint.InitialAddress;
      public IPAddress LastAddress => localEndpoint.LastAddress;
      public Guid Identifier => localEndpoint.Identifier;
      public string Name => localEndpoint.Name;
      public IReadOnlyDictionary<Guid, byte[]> EnumerateProperties() => localEndpoint.EnumerateProperties();
      public TValue GetProperty<TValue>(Guid key) => localEndpoint.GetProperty<TValue>(key);
      public TValue GetPropertyOrDefault<TValue>(Guid key) => localEndpoint.GetPropertyOrDefault<TValue>(key);
      public bool TryGetProperty<TValue>(Guid key, out TValue value) => localEndpoint.TryGetProperty(key, out value);
      public bool Matches(Guid recipientId) => localEndpoint.Matches(recipientId);
      public int GetRevisionNumber() => localEndpoint.GetRevisionNumber();
      public void SetProperty<TValue>(Guid key, TValue value) => localEndpoint.SetProperty(key, value);
      #endregion

      #region MessageSender
      public void SendReliableUnicast<TMessage>(Guid recipientId, TMessage payload, MessagePriority priority) => messageSender.SendReliableUnicast(recipientId, payload, priority);
      public void SendUnreliableUnicast<TMessage>(Guid recipientId, TMessage message) => messageSender.SendUnreliableUnicast(recipientId, message);
      public void SendBroadcast<TMessage>(TMessage payload) => messageSender.SendBroadcast(payload);
      #endregion

      #region MessageRouter
      public void RegisterPayloadHandler<T>(Action<IReceivedMessage<T>> handler) => messageRouter.RegisterPayloadHandler(handler);
      public void RegisterPayloadHandler(Type t, Action<IReceivedMessage<object>> handler) => messageRouter.RegisterPayloadHandler(t, handler);
      public void RouteMessage<TPayload>(IReceivedMessage<TPayload> receivedMessage) => messageRouter.RouteMessage(receivedMessage);
      public void RouteMessage(Type payloadType, IReceivedMessage<object> receivedMessage) => messageRouter.RouteMessage(payloadType, receivedMessage);
      #endregion

      #region ReadablePeerRegistry
      public ReadableCourierEndpoint GetRemoteCourierEndpointOrNull(Guid identifier) => peerRegistry.GetRemoteCourierEndpointOrNull(identifier);
      public IEnumerable<ReadableCourierEndpoint> EnumeratePeers() => peerRegistry.EnumeratePeers();
      #endregion
   }
}
