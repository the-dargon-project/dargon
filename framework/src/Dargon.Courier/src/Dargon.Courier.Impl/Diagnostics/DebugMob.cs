using System.Linq;
using System.Runtime.InteropServices;
using Dargon.Courier.PeeringTier;
using Dargon.Courier.RoutingTier;

namespace Dargon.Courier {
   [Guid("09BEEB55-9D58-492F-97BB-0BAEF4EE5BD2")]
   public class DebugMob {
      private readonly RoutingTable routingTable;
      private readonly PeerTable peerTable;

      public DebugMob(RoutingTable routingTable, PeerTable peerTable) {
         this.routingTable = routingTable;
         this.peerTable = peerTable;
      }

      [ManagedProperty(IsDataSource = true)]
      public int RouteCount => routingTable.Enumerate().Count();

      [ManagedProperty(IsDataSource = true)]
      public int PeerCount => peerTable.Enumerate().Count();
   }
}
