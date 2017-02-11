using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dargon.Courier.TransportTier.Udp {
   public static class DataSetNames {
      public const string kInboundBytes = "courier.transport.udp.inboundBytes";
      public const string kOutboundBytes = "courier.transport.udp.outboundBytes";
      public const string kInboundProcessDispatchLatency = "courier.transport.udp.inboundProcessDispatchLatency";
      public const string kTotalResends = "courier.transport.udp.total_resends";
      public const string kMessageResends = "courier.transport.udp.message_resends";
      public const string kTossed = "courier.transport.udp.tossed";
      public const string kDuplicatesReceived = "courier.transport.udp.duplicatesReceived";
      public const string kAnnouncementsReceived = "courier.transport.udp.announcementsReceived";
      public const string kMultiPartChunksSent = "courier.transport.udp.multiPartChunksSent";
      public const string kMultiPartChunksBytesReceived = "courier.transport.udp.multiPartChunksBytesReceived";
      public const string kOutboundMessageRateLimit = "courier.transport.udp.outboundMessageRateLimit";
      public const string kSendQueueDepth = "courier.transport.udp.sendQueueDepth";
   }
}
