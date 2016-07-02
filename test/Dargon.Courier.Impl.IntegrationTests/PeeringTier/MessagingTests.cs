using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Courier.AsyncPrimitives;
using Dargon.Courier.TransportTier.Tcp;
using Dargon.Courier.TransportTier.Test;
using Dargon.Courier.TransportTier.Udp;
using Dargon.Courier.Vox;
using Dargon.Vox;
using NLog;
using NMockito;
using Xunit;

namespace Dargon.Courier.PeeringTier {
   public abstract class MessagingTestsBase : NMockitoInstance {
      private readonly Logger logger = LogManager.GetCurrentClassLogger();

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
                  await TaskEx.YieldToThreadPool();

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
            await receiverFacade.ShutdownAsync().ConfigureAwait(false);
            await senderFacade.ShutdownAsync().ConfigureAwait(false);

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
                  await TaskEx.YieldToThreadPool();

                  AssertEquals(str, x.Body);
                  latch.Set();
               });

               await receiverFacade.PeerTable.GetOrAdd(senderFacade.Identity.Id).WaitForDiscoveryAsync(timeout.Token).ConfigureAwait(false);
               await senderFacade.PeerTable.GetOrAdd(receiverFacade.Identity.Id).WaitForDiscoveryAsync(timeout.Token).ConfigureAwait(false);

               await senderFacade.Messenger.SendReliableAsync(str, receiverFacade.Identity.Id).ConfigureAwait(false);
               await latch.WaitAsync(timeout.Token).ConfigureAwait(false);
            }
         } catch (Exception e) {
            Console.WriteLine("Threw " + e);
            throw;
         } finally {
            await receiverFacade.ShutdownAsync().ConfigureAwait(false);
            await senderFacade.ShutdownAsync().ConfigureAwait(false);

            AssertEquals(0, receiverFacade.RoutingTable.Enumerate().Count());
            AssertEquals(0, senderFacade.RoutingTable.Enumerate().Count());
         }
      }

      [Fact]
      public async Task LargeObjectTest() {
         try {
            var sw = new Stopwatch();
            sw.Start();

            logger.Info("Building 10MB large payload");
            var gigabytePayload = new byte[10 * 1000 * 1000]; //Util.Generate(1000 * 1000 * 1000, i => (byte)i);
            logger.Info("Done building large payload");

            //            var ms = new MemoryStream();
            //            Serialize.To(ms, new MessageDto { Body = gigabytePayload });
            //            ms.Position = 0;
            //            var mDto = (MessageDto)Deserialize.From(ms);
            //            AssertTrue(Util.ByteArraysEqual(gigabytePayload, (byte[])mDto.Body));

            using (var timeout = new CancellationTokenSource(1000000)) {
               // ensure the serialized object is large.
               var readCompletionLatch = new AsyncLatch();
               var router = receiverFacade.InboundMessageRouter;
               router.RegisterHandler<byte[]>(async x => {
                  await TaskEx.YieldToThreadPool();

                  logger.Info("Received large payload.");
                  var equalityCheckResult = Util.ByteArraysEqual(gigabytePayload, x.Body);
                  logger.Info("Validation result: " + equalityCheckResult);
                  AssertTrue(equalityCheckResult);
                  logger.Info("Validated large payload.");
                  readCompletionLatch.Set();
               });

               await receiverFacade.PeerTable.GetOrAdd(senderFacade.Identity.Id).WaitForDiscoveryAsync(timeout.Token);
               await senderFacade.PeerTable.GetOrAdd(receiverFacade.Identity.Id).WaitForDiscoveryAsync(timeout.Token);

               logger.Info("Sending large payload");
               await senderFacade.Messenger.SendReliableAsync(gigabytePayload, receiverFacade.Identity.Id);
               logger.Info("Sent large payload");
               await readCompletionLatch.WaitAsync(timeout.Token);
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
