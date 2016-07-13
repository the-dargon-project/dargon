using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Dargon.Courier.TransportTier.Udp {
   public class InboundDataEvent {
      public UdpClient UdpClient { get; set; }
      public SocketAsyncEventArgs SocketArgs { get; set; }
      public byte[] Data { get; set; }
      public int DataOffset { get; set; }
      public int DataLength { get; set; }
      public UdpClientRemoteInfo RemoteInfo { get; set; }
      public Stopwatch StopWatch { get; set; }
   }
}
