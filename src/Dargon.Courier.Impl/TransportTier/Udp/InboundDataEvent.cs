using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Dargon.Courier.TransportTier.Udp {
   public class InboundDataEvent {
      public byte[] Data { get; set; }
      public int DataOffset { get; set; }
      public int DataLength { get; set; }
      public UdpClientRemoteInfo RemoteInfo { get; set; }
   }
}
