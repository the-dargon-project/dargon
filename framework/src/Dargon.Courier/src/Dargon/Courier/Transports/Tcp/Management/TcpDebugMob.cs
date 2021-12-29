using Dargon.Courier.AuditingTier;
using System.Linq;
using System.Runtime.InteropServices;

namespace Dargon.Courier.TransportTier.Tcp.Management {
   [ManagedDataSet("inbound_bytes", DataSetNames.kInboundBytes, typeof(IAuditAggregator<double>))]
   [ManagedDataSet("outbound_bytes", DataSetNames.kOutboundBytes, typeof(IAuditAggregator<double>))]
   [Guid("2170CAA2-A8FF-40F2-84F9-ED648B83D0C7")] // Should TcpDebugMobs be queried by name? More than one TCP transport?
   public class TcpDebugMob {
      private readonly TcpRoutingContextContainer tcpRoutingContextContainer;

      public TcpDebugMob(TcpRoutingContextContainer tcpRoutingContextContainer) {
         this.tcpRoutingContextContainer = tcpRoutingContextContainer;
      }

      [ManagedProperty(IsDataSource = true)]
      public int RoutingContextCount => tcpRoutingContextContainer.Enumerate().Count();
   }
}
