using Dargon.Courier.Identities;
using Dargon.Courier.Messaging;
using Dargon.Courier.Networking;
using Dargon.Courier.PortableObjects;
using Dargon.PortableObjects;
using ItzWarty;
using ItzWarty.Networking;
using ItzWarty.Pooling;
using ItzWarty.Threading;
using System;
using System.Net;
using Dargon.Courier.Peering;

namespace Dargon.Courier {
   public interface CourierClientFactory {
      CourierClient CreateUdpCourierClient(int port, CourierClientConfiguration clientConfiguration = null);
   }

   public class CourierClientConfiguration {
      public Guid Identifier { get; set; } = Guid.Empty;
      public string Name { get; set; } = null;
   }

   public class CourierClientFactoryImpl : CourierClientFactory {
      private readonly GuidProxy guidProxy;
      private readonly IThreadingProxy threadingProxy;
      private readonly INetworkingProxy networkingProxy;
      private readonly ObjectPoolFactory objectPoolFactory;
      private readonly IPofSerializer pofSerializer;

      public CourierClientFactoryImpl(GuidProxy guidProxy, IThreadingProxy threadingProxy, INetworkingProxy networkingProxy, ObjectPoolFactory objectPoolFactory, IPofSerializer pofSerializer) {
         this.guidProxy = guidProxy;
         this.threadingProxy = threadingProxy;
         this.networkingProxy = networkingProxy;
         this.objectPoolFactory = objectPoolFactory;
         this.pofSerializer = pofSerializer;
      }

      public CourierClient CreateUdpCourierClient(int port, CourierClientConfiguration clientConfiguration = null) {
         clientConfiguration = clientConfiguration ?? new CourierClientConfiguration();
         InitializeDefaults($"udp({port})", clientConfiguration);

         var endpoint = new CourierEndpointImpl(pofSerializer, clientConfiguration.Identifier, clientConfiguration.Name);
         var network = new UdpCourierNetwork(networkingProxy, new UdpCourierNetworkConfiguration(port));
         var networkContext = network.Join(endpoint);

         var networkBroadcaster = new NetworkBroadcasterImpl(endpoint, networkContext, pofSerializer);
         var messageContextPool = objectPoolFactory.CreatePool(() => new UnacknowledgedReliableMessageContext());
         var unacknowledgedReliableMessageContainer = new UnacknowledgedReliableMessageContainer(messageContextPool);
         var messageDtoPool = objectPoolFactory.CreatePool(() => new CourierMessageV1());
         var messageTransmitter = new MessageTransmitterImpl(guidProxy, pofSerializer, networkBroadcaster, unacknowledgedReliableMessageContainer, messageDtoPool);
         var messageSender = new MessageSenderImpl(guidProxy, unacknowledgedReliableMessageContainer, messageTransmitter);
         var acknowledgeDtoPool = objectPoolFactory.CreatePool(() => new CourierMessageAcknowledgeV1());
         var messageAcknowledger = new MessageAcknowledgerImpl(networkBroadcaster, unacknowledgedReliableMessageContainer, acknowledgeDtoPool);
         var periodicAnnouncer = new PeriodicAnnouncerImpl(threadingProxy, pofSerializer, endpoint, networkBroadcaster);
         periodicAnnouncer.Start();
         var periodicResender = new PeriodicResenderImpl(threadingProxy, unacknowledgedReliableMessageContainer, messageTransmitter);
         periodicResender.Start();

         ReceivedMessageFactory receivedMessageFactory = new ReceivedMessageFactoryImpl(pofSerializer);
         MessageRouter messageRouter = new MessageRouterImpl();
         var peerRegistry = new PeerRegistryImpl(pofSerializer);
         var networkReceiver = new NetworkReceiverImpl(endpoint, networkContext, pofSerializer, messageRouter, messageAcknowledger, peerRegistry, receivedMessageFactory);
         networkReceiver.Initialize();

         return new CourierClientFacadeImpl(endpoint, messageSender, messageRouter, peerRegistry);
      }

      private void InitializeDefaults(string tag, CourierClientConfiguration clientConfiguration) {
         if (clientConfiguration.Identifier.Equals(Guid.Empty)) {
            clientConfiguration.Identifier = Guid.NewGuid();
         }
         if (clientConfiguration.Name == null) {
            var hostname = Dns.GetHostName();
            clientConfiguration.Name = hostname + "_" + tag;
         }
      }
   }
}
