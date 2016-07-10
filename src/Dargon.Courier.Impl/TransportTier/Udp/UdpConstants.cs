using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dargon.Courier.TransportTier.Udp {
   public static class UdpConstants {
      public const int kMaximumTransportSize = 2048;
      public const int kMultiPartChunkSize = 512;

      public const string kUnicastPortIdentityPropertyKey = "udp_unicast_receive_port";
   }
}
