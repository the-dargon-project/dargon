using System;
using Dargon.Commons;
using Dargon.Courier.TransportTier.Udp;
using NMockito;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Courier.TransportTier.Tcp;
using Dargon.Courier.TransportTier.Test;
using Nito.AsyncEx;
using static Dargon.Commons.Channels.ChannelsExtensions;

namespace Dargon.Courier.TransportTier {
   public class MessagingLoadTestsBase : NMockitoInstance {
      private IReadOnlyList<CourierFacade> courierFacades;

      public void Setup(IReadOnlyList<CourierFacade> courierFacades) {
         this.courierFacades = courierFacades;
      }

      public async Task RunAsync() {
         Console.WriteLine(DateTime.Now);
         const int kMessagesPerWorker = 2000;
         var sink = courierFacades[0];
         var senders = courierFacades.Skip(1).ToArray();
         var counter = kMessagesPerWorker * senders.Length;
         var doneSignal = new AsyncManualResetEvent();
         int upCounter = 0;
         sink.InboundMessageRouter.RegisterHandler<bool>(
            x => {
               var newCounter = Interlocked.Decrement(ref counter);
               Interlocked.Increment(ref upCounter);
//               Console.Title = newCounter + " " + upCounter;
               if (newCounter == 0) {
                  doneSignal.Set();
               }
               return Task.FromResult(false);
            });

         var sync = new AsyncCountdownEvent(senders.Length);
         var senderTasks = senders.Select(s => Go(async () => {
            await s.PeerTable.GetOrAdd(sink.Identity.Id).WaitForDiscoveryAsync().ConfigureAwait(false);
            sync.Signal();
            await sync.WaitAsync().ConfigureAwait(false);
            Console.WriteLine("Sink discovered: " + DateTime.Now);

            for (var asdf = 0; asdf < 20; asdf++) {
               await Task.WhenAll(Util.Generate(
                  kMessagesPerWorker / 20,
                  i => s.Messenger.SendReliableAsync(
                     true,
                     sink.Identity.Id))
                  ).ConfigureAwait(false);
            }

            Console.WriteLine("Worker Done: " + DateTime.Now);
         }));
         await Task.WhenAll(senderTasks).ConfigureAwait(false);
         Console.WriteLine("Senders Done: " + DateTime.Now);

         await doneSignal.WaitAsync().ConfigureAwait(false);
         Console.WriteLine("Done Signalled: " + DateTime.Now);
      }
   }

   public class LocalMessagingLoadTests : MessagingLoadTestsBase {
      public LocalMessagingLoadTests() {
         var testTransportFactory = new TestTransportFactory();
         var courierFacades = Util.Generate(
            3,
            i => CourierBuilder.Create()
                               .UseTransport(testTransportFactory)
                               .UseTcpServerTransport(21337 + i)
                               .BuildAsync().Result);
         Setup(courierFacades);
      }
   }

   public class UdpMessagingLoadTests : MessagingLoadTestsBase {
      public UdpMessagingLoadTests() {
         var courierFacades = Util.Generate(
            3,
            i => CourierBuilder.Create()
                               .UseUdpMulticastTransport()
                               .UseTcpServerTransport(21337 + i)
                               .BuildAsync().Result);
         Setup(courierFacades);
      }
   }
}