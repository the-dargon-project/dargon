using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Dargon.Courier.TransportTier.Udp {
   public class UdpFacade {
      private readonly CoreUdp coreUdp;
      private readonly UdpDispatcher udpDispatcher;
      private readonly CancellationTokenSource shutdownCts;

      public UdpFacade(CoreUdp coreUdp, UdpDispatcher udpDispatcher, CancellationTokenSource shutdownCts) {
         this.coreUdp = coreUdp;
         this.udpDispatcher = udpDispatcher;
         this.shutdownCts = shutdownCts;
      }

      public async Task ShutdownAsync() {
         coreUdp.Shutdown();
         udpDispatcher.Shutdown();
         shutdownCts.Cancel();
      }
   }
}
