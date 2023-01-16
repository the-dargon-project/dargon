using Dargon.Commons;
using Dargon.Courier.TransportTier.Tcp;
using Dargon.Courier.TransportTier.Test;
using Dargon.Courier.TransportTier.Udp;
using NMockito;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons.AsyncPrimitives;
using Dargon.Commons.Channels;
using Dargon.Commons.Collections;
using Dargon.Courier.AccessControlTier;
using static Dargon.Commons.Channels.ChannelsExtensions;

namespace Dargon.Courier.TransportTier {
   public class MessagingLoadTestsBase : NMockitoInstance {
      private IReadOnlyList<CourierFacade> courierFacades;

      public void Setup(IReadOnlyList<CourierFacade> courierFacades) {
         this.courierFacades = courierFacades;
      }

      public async Task RunAsync() {
         Console.WriteLine(DateTime.Now);
         const int kMessagesPerWorker = 200000;
         var sink = courierFacades[0];
         var senders = courierFacades.Skip(1).ToArray();
         var counter = kMessagesPerWorker * senders.Length;
         var doneSignal = new AsyncLatch();
         int upCounter = 0;
         var set = new ConcurrentSet<int>();
         sink.InboundMessageRouter.RegisterHandler<string>(
            x => {
               set.AddOrThrow(int.Parse(x.Body));
               var newCounter = Interlocked.Decrement(ref counter);
               Interlocked.Increment(ref upCounter);
               if (upCounter % 500 == 0)
                  Console.WriteLine(newCounter + " " + upCounter);
               if (newCounter == 0) {
                  doneSignal.SetOrThrow();
               }
               return Task.FromResult(false);
            });

         var sync = new AsyncCountdownLatch(senders.Length);
         var senderTasks = senders.Select((s, id) => Go(async () => {
            await s.PeerTable.GetOrAdd(sink.Identity.Id).WaitForDiscoveryAsync().ConfigureAwait(false);
            sync.Signal();
            await sync.WaitAsync().ConfigureAwait(false);
            Console.WriteLine("Sink discovered: " + DateTime.Now);

            const int kBatchFactor = 1;
            for (var batch = 0; batch < kBatchFactor; batch++) {
               var batchSize = kMessagesPerWorker / kBatchFactor;
               await Task.WhenAll(Arrays.Create(
                  batchSize,
                  i => s.Messenger.SendReliableAsync(
                     "" + (batch * batchSize + i + id * kMessagesPerWorker),
                     sink.Identity.Id))
                  ).ConfigureAwait(false);
            }

            Console.WriteLine("Worker Done: " + DateTime.Now);
         }));
         await Task.WhenAll(senderTasks).ConfigureAwait(false);
         Console.WriteLine("Senders Done: " + DateTime.Now);

         await doneSignal.WaitAsync().ConfigureAwait(false);
         Console.WriteLine("Done Signalled: " + DateTime.Now);

         AssertCollectionDeepEquals(set, new ConcurrentSet<int>(Enumerable.Range(0, kMessagesPerWorker * senders.Length)));

         while (true) {
            GC.Collect();
         }
      }
   }

   public class LocalMessagingLoadTests : MessagingLoadTestsBase {
      public LocalMessagingLoadTests() {
         var testTransportFactory = new TestTransportFactory();
         var courierFacades = Arrays.Create(
            3,
            i => CourierBuilder.Create()
                               .UseTransport(testTransportFactory)
                               .UseTcpServerTransport(21337 + i)
                               .UseGatekeeper(new PermitAllGatekeeper())
                               .BuildAsync().Result);
         Setup(courierFacades);
      }
   }

   public class UdpMessagingLoadTests : MessagingLoadTestsBase {
      public UdpMessagingLoadTests() {
         Go(async () => {
            var ch = new BlockingChannel<string>();
            var l = new AsyncLatch();
            Go(async () => {
               await l.WaitAsync().ConfigureAwait(false);
               for (var i = 0; i < 10; i++) {
                  await ch.WriteAsync("asdf").ConfigureAwait(false);
                  await Task.Delay(400).ConfigureAwait(false);
               }
            }).Forget();
            l.SetOrThrow();
            for (var i = 0; i < 10; i++) {
               await new Select {
                  Case(Time.After(500), () => {
                     Console.WriteLine("Nay!");
                  }),
                  Case(ch, () => {
                     Console.WriteLine("Yay!");
                  })
               }.WaitAsync().ConfigureAwait(false);
            }
            while (true) ;
         }).Wait();
         var courierFacades = Arrays.Create(
            2,
            i => CourierBuilder.Create()
                               .UseUdpTransport()
                               .UseTcpServerTransport(21337 + i)
                               .UseGatekeeper(new PermitAllGatekeeper())
                               .BuildAsync().Result);
         Setup(courierFacades);
      }
   }
}