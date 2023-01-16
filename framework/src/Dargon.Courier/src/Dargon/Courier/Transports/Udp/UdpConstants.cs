using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dargon.Courier.TransportTier.Udp {
   public static class UdpConstants {
      public const int kMaximumTransportSize = 8192;
      public const int kMultipartChunkOverhead = 256;
      public const int kMultiPartChunkSize = kMaximumTransportSize - kMultipartChunkOverhead; // Leave some space for headers.
      public const int kSmallFrameSize = 512;
      public const int kAckSerializationBufferSize = 64;

      public const string kUnicastPortIdentityPropertyKey = "udp_unicast_receive_port";
   }
}
