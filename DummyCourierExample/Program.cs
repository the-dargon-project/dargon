using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Courier.Identities;
using Dargon.Courier.Messaging;
using Dargon.Courier.Networking;
using Dargon.Courier.Peering;
using Dargon.Courier.PortableObjects;
using Dargon.PortableObjects;
using ItzWarty;
using ItzWarty.Collections;
using ItzWarty.IO;
using ItzWarty.Networking;
using ItzWarty.Pooling;
using ItzWarty.Threading;

namespace DummyCourierExample {
   public static class Program {
      private static void Main(string[] args) {
         //var network = new LocalCourierNetwork(dropRate: 0.1);
         IDnsProxy dnsProxy = new DnsProxy();
         var tcpEndPointFactory = new TcpEndPointFactory(dnsProxy);
         IThreadingFactory threadingFactory = new ThreadingFactory();
         ISynchronizationFactory synchronizationFactory = new SynchronizationFactory();
         IThreadingProxy threadingProxy = new ThreadingProxy(threadingFactory, synchronizationFactory);
         IStreamFactory streamFactory = new StreamFactory();
         INetworkingInternalFactory networkingInternalFactory = new NetworkingInternalFactory(threadingProxy, streamFactory);
         ISocketFactory socketFactory = new SocketFactory(tcpEndPointFactory, networkingInternalFactory);
         INetworkingProxy networkingProxy = new NetworkingProxy(socketFactory, tcpEndPointFactory);
         var network = new UdpCourierNetwork(networkingProxy, new UdpCourierNetworkConfiguration(50555));
         var tasks = Util.Generate(4, i => new Thread(() => EntryPoint(i, network)).With(t => t.Start()));
         tasks.ForEach(t => t.Join());
      }

      public static void EntryPoint(int i, CourierNetwork network) {
         ICollectionFactory collectionFactory = new CollectionFactory();
         ObjectPoolFactory objectPoolFactory = new DefaultObjectPoolFactory(collectionFactory);
         IThreadingFactory threadingFactory = new ThreadingFactory();
         ISynchronizationFactory synchronizationFactory = new SynchronizationFactory();
         IThreadingProxy threadingProxy = new ThreadingProxy(threadingFactory, synchronizationFactory);
         GuidProxy guidProxy = new GuidProxyImpl();
         IPofContext courierPofContext = new DargonCourierImplPofContext(1337);
         IPofSerializer courierSerializer = new PofSerializer(courierPofContext);
         Guid localIdentifier = guidProxy.NewGuid();
         var endpoint = new CourierEndpointImpl(courierSerializer, localIdentifier, "node" + i);
         var networkContext = network.Join(endpoint);

         var networkBroadcaster = new NetworkBroadcasterImpl(endpoint, networkContext, courierSerializer);
         var messageContextPool = objectPoolFactory.CreatePool(() => new UnacknowledgedReliableMessageContext());
         var unacknowledgedReliableMessageContainer = new UnacknowledgedReliableMessageContainer(messageContextPool);
         var messageDtoPool = objectPoolFactory.CreatePool(() => new CourierMessageV1());
         var messageTransmitter = new MessageTransmitterImpl(guidProxy, courierSerializer, networkBroadcaster, unacknowledgedReliableMessageContainer, messageDtoPool);
         var messageSender = new MessageSenderImpl(guidProxy, unacknowledgedReliableMessageContainer, messageTransmitter);
         var acknowledgeDtoPool = objectPoolFactory.CreatePool(() => new CourierMessageAcknowledgeV1());
         var messageAcknowledger = new MessageAcknowledgerImpl(networkBroadcaster, unacknowledgedReliableMessageContainer, acknowledgeDtoPool);
         var periodicAnnouncer = new PeriodicAnnouncerImpl(threadingProxy, courierSerializer, endpoint, networkBroadcaster);
         periodicAnnouncer.Start();
         var periodicResender = new PeriodicResenderImpl(threadingProxy, unacknowledgedReliableMessageContainer, messageTransmitter);
         periodicResender.Start();

         ReceivedMessageFactory receivedMessageFactory = new ReceivedMessageFactoryImpl(courierSerializer);
         MessageRouter messageRouter = new MessageRouterImpl(receivedMessageFactory);
         var peerRegistry = new PeerRegistryImpl(courierSerializer);
         var networkReceiver = new NetworkReceiverImpl(endpoint, networkContext, courierSerializer, messageRouter, messageAcknowledger, peerRegistry);
         networkReceiver.Initialize();

         messageRouter.RegisterPayloadHandler<string>(m => {
//            Console.WriteLine(i + ": " + m.Payload);
         });

         Thread.Sleep(3000);

         if (i == 0) {
            while (true) {
               for (var j = 0; j < 50; j++) {
                  Console.WriteLine(unacknowledgedReliableMessageContainer.GetUnsentMessagesRemaining() + " pending");
                  var stopwatch = new Stopwatch();
                  stopwatch.Start();
                  var messagesRemaining = unacknowledgedReliableMessageContainer.GetUnsentMessagesRemaining();
                  var peers = peerRegistry.EnumeratePeers().ToArray();
                  for (var k = 0; k < 10000; k++) {
                     foreach (var peer in peers) {
                        messageSender.SendReliableUnicast(peer.Id, "Message " + j + " hello from " + i + ", " + peer.Id, MessagePriority.Low);
                     }
                  }
                  var messagesAcked = unacknowledgedReliableMessageContainer.GetUnsentMessagesRemaining() - messagesRemaining + 10000;
                  Console.WriteLine("Got " + (messagesAcked * peers.Length) + " acks in " + stopwatch.ElapsedMilliseconds + "ms (" + (messagesAcked * peers.Length / stopwatch.Elapsed.TotalSeconds) + " per second) " + messagesRemaining + " remaining");
                  Thread.Sleep(1);
               }

               for (var j = 0; j < 1000; j++) {
                  Console.WriteLine(unacknowledgedReliableMessageContainer.GetUnsentMessagesRemaining() + " pending");
                  Thread.Sleep(1);
               }
            }
         }
      }
   }
}
