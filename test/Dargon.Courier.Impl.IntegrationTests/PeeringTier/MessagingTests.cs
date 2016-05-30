using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Courier.AsyncPrimitives;
using Dargon.Courier.TransportTier.Tcp;
using Dargon.Courier.TransportTier.Test;
using Dargon.Courier.TransportTier.Udp;
using NMockito;
using Xunit;

namespace Dargon.Courier.PeeringTier {
   public abstract class MessagingTestsBase : NMockitoInstance {
      private CourierFacade senderFacade;
      private CourierFacade receiverFacade;

      public void Setup(CourierFacade senderFacade, CourierFacade receiverFacade) {
         this.senderFacade = senderFacade;
         this.receiverFacade = receiverFacade;
      }

      [Fact]
      public async Task BroadcastTest() {
         try {
            using (var timeout = new CancellationTokenSource(2000)) {
               var str = CreatePlaceholder<string>();

               var latch = new AsyncLatch();
               receiverFacade.InboundMessageRouter.RegisterHandler<string>(async x => {
                  await Task.Yield();

                  AssertEquals(str, x.Body);
                  latch.Set();
               });

               // await discovery between nodes
               await receiverFacade.PeerTable.GetOrAdd(senderFacade.Identity.Id).WaitForDiscoveryAsync(timeout.Token);
               await senderFacade.PeerTable.GetOrAdd(receiverFacade.Identity.Id).WaitForDiscoveryAsync(timeout.Token);

               await senderFacade.Messenger.BroadcastAsync(str);
               await latch.WaitAsync(timeout.Token);
            }
         } catch (Exception e) {
            Console.WriteLine("Threw " + e);
            throw;
         } finally {
            await receiverFacade.ShutdownAsync();
            await senderFacade.ShutdownAsync();

            AssertEquals(0, receiverFacade.RoutingTable.Enumerate().Count());
            AssertEquals(0, senderFacade.RoutingTable.Enumerate().Count());
         }
      }

      [Fact]
      public async Task ReliableTest() {
         try {
            using (var timeout = new CancellationTokenSource(10000)) {

               var str = CreatePlaceholder<string>();

               var latch = new AsyncLatch();
               var router = receiverFacade.InboundMessageRouter;
               router.RegisterHandler<string>(async x => {
                  await Task.Yield();

                  AssertEquals(str, x.Body);
                  latch.Set();
               });

               await receiverFacade.PeerTable.GetOrAdd(senderFacade.Identity.Id).WaitForDiscoveryAsync(timeout.Token);
               await senderFacade.PeerTable.GetOrAdd(receiverFacade.Identity.Id).WaitForDiscoveryAsync(timeout.Token);

               await senderFacade.Messenger.SendReliableAsync(str, receiverFacade.Identity.Id);
               await latch.WaitAsync(timeout.Token);
            }
         } catch (Exception e) {
            Console.WriteLine("Threw " + e);
            throw;
         } finally {
            await receiverFacade.ShutdownAsync();
            await senderFacade.ShutdownAsync();

            AssertEquals(0, receiverFacade.RoutingTable.Enumerate().Count());
            AssertEquals(0, senderFacade.RoutingTable.Enumerate().Count());
         }
      }
   }

   public class LocalMessagingTests : MessagingTestsBase {
      public LocalMessagingTests() {
         var testTransportFactory = new TestTransportFactory();

         var senderfacade = CourierBuilder.Create()
                                          .UseTransport(testTransportFactory)
                                          .BuildAsync().Result;

         var receiverFacade = CourierBuilder.Create()
                                            .UseTransport(testTransportFactory)
                                            .BuildAsync().Result;

         Setup(senderfacade, receiverFacade);
      }
   }

   public class UdpMessagingTests : MessagingTestsBase {
      public UdpMessagingTests() {
         var senderFacade = CourierBuilder.Create()
                                          .UseUdpMulticastTransport()
                                          .BuildAsync().Result;
         var receiverFacade = CourierBuilder.Create()
                                            .UseUdpMulticastTransport()
                                            .BuildAsync().Result;
         Setup(senderFacade, receiverFacade);
      }
   }

   public class TcpMessagingTests : MessagingTestsBase {
      public TcpMessagingTests() {
         var senderFacade = CourierBuilder.Create()
                                          .UseTcpClientTransport(IPAddress.Loopback, 21337)
                                          .BuildAsync().Result;

         var receiverFacade = CourierBuilder.Create()
                                            .UseTcpServerTransport(21337)
                                            .BuildAsync().Result;

         Setup(senderFacade, receiverFacade);
      }
   }
}
