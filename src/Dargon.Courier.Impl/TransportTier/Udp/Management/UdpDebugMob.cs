using Dargon.Courier.AuditingTier;
using System;
using System.Runtime.InteropServices;

namespace Dargon.Courier.TransportTier.Udp.Management {
   [ManagedDataSet("inbound_bytes", DataSetNames.kInboundBytes, typeof(AuditAggregator<double>))]
   [ManagedDataSet("outbound_bytes", DataSetNames.kOutboundBytes, typeof(AuditAggregator<double>))]
   [ManagedDataSet("resends", DataSetNames.kResends, typeof(AuditAggregator<int>))]
   [ManagedDataSet("tossed", DataSetNames.kTossed, typeof(AuditCounter))]
   [ManagedDataSet("duplicates_received", DataSetNames.kDuplicatesReceived, typeof(AuditCounter))]
   [ManagedDataSet("announcements_received", DataSetNames.kAnnouncementsReceived, typeof(AuditCounter))]
   [Guid("CA65B41E-DB31-4EDE-81BF-525BED6747A1")]
   public class UdpDebugMob {

   }
}
