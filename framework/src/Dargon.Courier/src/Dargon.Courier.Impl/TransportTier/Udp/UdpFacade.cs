using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Dargon.Courier.TransportTier.Udp {
   public class UdpFacade {
      private readonly UdpClient client;
      private readonly UdpDispatcherImpl udpDispatcher;
      private readonly CancellationTokenSource shutdownCts;

      public UdpFacade(UdpClient client, UdpDispatcherImpl udpDispatcher, CancellationTokenSource shutdownCts) {
         this.client = client;
         this.udpDispatcher = udpDispatcher;
         this.shutdownCts = shutdownCts;
      }

      public async Task ShutdownAsync() {
         client.Shutdown();
         udpDispatcher.Shutdown();
         shutdownCts.Cancel();
      }
   }
}
