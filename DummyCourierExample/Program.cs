using System;
using System.Collections.Generic;
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
using ItzWarty.Threading;

namespace DummyCourierExample {
   public static class Program {
      private static void Main(string[] args) {
         var network = new LocalCourierNetwork(dropRate: 0.2);
         var tasks = Util.Generate(4, i => new Thread(() => EntryPoint(i, network)).With(t => t.Start()));
         tasks.ForEach(t => t.Join());
      }

      public static void EntryPoint(int i, CourierNetwork network) {
         IThreadingFactory threadingFactory = new ThreadingFactory();
         ISynchronizationFactory synchronizationFactory = new SynchronizationFactory();
         IThreadingProxy threadingProxy = new ThreadingProxy(threadingFactory, synchronizationFactory);
         GuidProxy guidProxy = new GuidProxyImpl();
         IPofContext courierPofContext = new DargonCourierImplPofContext(1337);
         IPofSerializer courierSerializer = new PofSerializer(courierPofContext);
         Guid localIdentifier = guidProxy.NewGuid();
         var endpoint = new CourierEndpointImpl(courierSerializer, localIdentifier, "node" + i);
         var networkContext = network.Join(endpoint);
         ReceivedMessageFactory receivedMessageFactory = new ReceivedMessageFactoryImpl(courierSerializer);
         MessageRouter messageRouter = new MessageRouterImpl(receivedMessageFactory);
         var peerRegistry = new PeerRegistryImpl(courierSerializer);
         var networkReceiver = new NetworkReceiverImpl(endpoint, networkContext, courierSerializer, messageRouter, peerRegistry);
         networkReceiver.Initialize();
         var networkBroadcaster = new NetworkBroadcasterImpl(endpoint, networkContext, courierSerializer);
         MessageTransmitter messageTransmitter = new MessageTransmitterImpl(guidProxy, courierSerializer, networkBroadcaster);
         var periodicAnnouncer = new PeriodicAnnouncerImpl(threadingProxy, courierSerializer, endpoint, networkBroadcaster);
         periodicAnnouncer.Start();

         messageRouter.RegisterPayloadHandler<string>(m => {
            Console.WriteLine(m.Guid + " " + m.Payload);
         });

         while (true) {
            messageTransmitter.SendBroadcast("Hello");
         }
      }
   }
}
