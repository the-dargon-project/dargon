using Dargon.Courier.AuditingTier;
using System;
using System.Runtime.InteropServices;

namespace Dargon.Courier.TransportTier.Udp.Management {
   [ManagedDataSet("inbound_bytes", DataSetNames.kInboundBytes, typeof(AuditAggregator<double>))]
   [ManagedDataSet("outbound_bytes", DataSetNames.kOutboundBytes, typeof(AuditAggregator<double>))]
   [ManagedDataSet("inbound_dispatch_latency", DataSetNames.kInboundProcessDispatchLatency, typeof(AuditAggregator<double>))]
   [ManagedDataSet("resends", DataSetNames.kResends, typeof(AuditAggregator<int>))]
   [ManagedDataSet("tossed", DataSetNames.kTossed, typeof(AuditCounter))]
   [ManagedDataSet("duplicates_received", DataSetNames.kDuplicatesReceived, typeof(AuditCounter))]
   [ManagedDataSet("announcements_received", DataSetNames.kAnnouncementsReceived, typeof(AuditCounter))]
   [ManagedDataSet("multipart_chunks_sent", DataSetNames.kMultiPartChunksSent, typeof(AuditCounter))]
   [ManagedDataSet("multipart_chunk_bytes_received", DataSetNames.kMultiPartChunksBytesReceived, typeof(AuditAggregator<int>))]
   [Guid("CA65B41E-DB31-4EDE-81BF-525BED6747A1")]
   public class UdpDebugMob {

   }
}
