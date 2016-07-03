using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dargon.Courier.TransportTier.Udp {
   public static class UdpConstants {
      public const int kMaximumTransportSize = 256; //8192;
      public const int kMultiPartChunkSize = 128; // 4096;
   }
}
