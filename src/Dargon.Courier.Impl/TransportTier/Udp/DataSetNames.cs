using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dargon.Courier.TransportTier.Udp {
   public static class DataSetNames {
      public const string kInboundBytes = "courier.transport.udp.inboundBytes";
      public const string kOutboundBytes = "courier.transport.udp.outboundBytes";
      public const string kResends = "courier.transport.udp.resends";
      public const string kTossed = "courier.transport.udp.tossed";
      public const string kDuplicatesReceived = "courier.transport.udp.duplicatesReceived";
      public const string kAnnouncementsReceived = "courier.transport.udp.announcementsReceived";
   }
}
