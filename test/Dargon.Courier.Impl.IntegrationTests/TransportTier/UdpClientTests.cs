using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Courier.AuditingTier;
using Dargon.Courier.TransportTier.Udp;
using NMockito;
using NMockito.Attributes;

namespace Dargon.Courier.TransportTier {
   public class UdpClientTests : NMockitoInstance {
      private readonly UdpClient sender;

      public UdpClientTests() {
         sender = UdpClient.Create(
            UdpTransportConfigurationBuilder.Create().Build(),
            new NullAuditAggregator<double>(), 
            new NullAuditAggregator<double>(), 
            new NullAuditAggregator<double>());
      }

      public void Run() {
         const int bufferSize = 256;
         const int messageCount = 1 << 20;

         var sendsCompletedLatch = new CountdownEvent(messageCount);

         var buffer = new MemoryStream();
         buffer.Write(Util.Generate(bufferSize, i => (byte)StaticRandom.Next(0, bufferSize)), 0, bufferSize);

         int messagesDispatched = 0;
         var dispatcher = new InlineUdpDispatcher(
            (e, returnCallback) => {
               returnCallback(e);
               Interlocked.Increment(ref messagesDispatched);
            });
         sender.StartReceiving(dispatcher);

         var sw = new Stopwatch();
         sw.Start();
         for (var i = 0; i < messageCount; i++) {
            sender.Broadcast(buffer, 0, bufferSize, () => sendsCompletedLatch.Signal());
         }
         Console.WriteLine("All " + messageCount + " broadcasts requested " + sw.ElapsedMilliseconds);

         sendsCompletedLatch.Wait();

         Console.WriteLine("All broadcasts complete " + sw.ElapsedMilliseconds);

         Thread.Sleep(1000);

         Console.WriteLine("Messages dispatched at " + sw.ElapsedMilliseconds + ": " + messagesDispatched);
      }

      public class InlineUdpDispatcher : IUdpDispatcher {
         private readonly Action<InboundDataEvent, Action<InboundDataEvent>> callback;

         public InlineUdpDispatcher(Action<InboundDataEvent, Action<InboundDataEvent>> callback) {
            this.callback = callback;
         }

         public void HandleInboundDataEvent(InboundDataEvent e, Action<InboundDataEvent> returnInboundDataEvent) {
            callback(e, returnInboundDataEvent);
         }
      }
   }
}
