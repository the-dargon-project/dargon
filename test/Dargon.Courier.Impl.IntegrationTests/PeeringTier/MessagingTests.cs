using System;
using System.Linq;
using System.Net;
using Dargon.Courier.AsyncPrimitives;
using Dargon.Courier.TransportTier.Test;
using Dargon.Ryu;
using NMockito;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Courier.PeeringTier;
using Dargon.Courier.RoutingTier;
using Dargon.Courier.TransportTier.Tcp.Server;
using Dargon.Courier.TransportTier.Udp;
using Xunit;

namespace Dargon.Courier {
   public abstract class MessagingTestsBase : NMockitoInstance {
      private IRyuContainer senderContainer;
      private IRyuContainer receiverContainer;

      public void Setup(IRyuContainer senderContainer, IRyuContainer receiverContainer) {
         this.senderContainer = senderContainer;
         this.receiverContainer = receiverContainer;
      }

      [Fact]
      public async Task BroadcastTest() {
         try {
            using (var timeout = new CancellationTokenSource(2000)) {
               var str = CreatePlaceholder<string>();

               var latch = new AsyncLatch();
               var router = receiverContainer.GetOrThrow<InboundMessageRouter>();
               router.RegisterHandler<string>(async x => {
                  await Task.Yield();

                  AssertEquals(str, x.Body);
                  latch.Set();
               });

               // await discovery between nodes
               await receiverContainer.GetOrThrow<PeerTable>().GetOrAdd(senderContainer.GetOrThrow<Identity>().Id).WaitForDiscoveryAsync(timeout.Token);
               await senderContainer.GetOrThrow<PeerTable>().GetOrAdd(receiverContainer.GetOrThrow<Identity>().Id).WaitForDiscoveryAsync(timeout.Token);

               await senderContainer.GetOrThrow<Messenger>().BroadcastAsync(str);
               await latch.WaitAsync(timeout.Token);
            }
         } catch (Exception e) {
            Console.WriteLine("Threw " + e);
            throw;
         } finally {
            await receiverContainer.GetOrThrow<CourierFacade>().ShutdownAsync();
            await senderContainer.GetOrThrow<CourierFacade>().ShutdownAsync();

            AssertEquals(0, receiverContainer.GetOrThrow<RoutingTable>().Enumerate().Count());
            AssertEquals(0, senderContainer.GetOrThrow<RoutingTable>().Enumerate().Count());
         }
      }

      [Fact]
      public async Task ReliableTest() {
         try {
            using (var timeout = new CancellationTokenSource(10000)) {

               var str = CreatePlaceholder<string>();

               var latch = new AsyncLatch();
               var router = receiverContainer.GetOrThrow<InboundMessageRouter>();
               router.RegisterHandler<string>(async x => {
                  await Task.Yield();

                  AssertEquals(str, x.Body);
                  latch.Set();
               });

               await receiverContainer.GetOrThrow<PeerTable>().GetOrAdd(senderContainer.GetOrThrow<Identity>().Id).WaitForDiscoveryAsync(timeout.Token);
               await senderContainer.GetOrThrow<PeerTable>().GetOrAdd(receiverContainer.GetOrThrow<Identity>().Id).WaitForDiscoveryAsync(timeout.Token);
               var messenger = senderContainer.GetOrThrow<Messenger>();
               await messenger.SendReliableAsync(str, receiverContainer.GetOrThrow<Identity>().Id);
               await latch.WaitAsync(timeout.Token);
            }
         } catch (Exception e) {
            Console.WriteLine("Threw " + e);
            throw;
         } finally {
            await receiverContainer.GetOrThrow<CourierFacade>().ShutdownAsync();
            await senderContainer.GetOrThrow<CourierFacade>().ShutdownAsync();
         }
      }
   }

   public class LocalMessagingTests : MessagingTestsBase {
      public LocalMessagingTests() {
         var root = new RyuFactory().Create();
         var testTransportFactory = new TestTransportFactory();
         var senderContainer = new CourierContainerFactory(root).CreateAsync(testTransportFactory).Result;
         var receiverContainer = new CourierContainerFactory(root).CreateAsync(testTransportFactory).Result;
         Setup(senderContainer, receiverContainer);
      }
   }

   public class UdpMessagingTests : MessagingTestsBase {
      public UdpMessagingTests() {
         var senderContainer = new CourierContainerFactory(new RyuFactory().Create()).CreateAsync(new UdpTransportFactory()).Result;
         var receiverContainer = new CourierContainerFactory(new RyuFactory().Create()).CreateAsync(new UdpTransportFactory()).Result;
         Setup(senderContainer, receiverContainer);
      }
   }

   public class TcpMessagingTests : MessagingTestsBase {
      public TcpMessagingTests() {
         var senderContainer = new CourierContainerFactory(new RyuFactory().Create()).CreateAsync(TcpTransportFactory.CreateClient(IPAddress.Loopback, 21337)).Result;
         var receiverContainer = new CourierContainerFactory(new RyuFactory().Create()).CreateAsync(TcpTransportFactory.CreateServer(21337)).Result;
         Setup(senderContainer, receiverContainer);
      }
   }
}
