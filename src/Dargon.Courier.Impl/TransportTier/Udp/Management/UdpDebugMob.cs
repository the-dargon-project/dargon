using Dargon.Courier.AuditingTier;
using System;
using System.Runtime.InteropServices;

namespace Dargon.Courier.TransportTier.Udp.Management {
   [ManagedDataSet("inbound_bytes", DataSetNames.kInboundBytes, typeof(IAuditAggregator<double>))]
   [ManagedDataSet("outbound_bytes", DataSetNames.kOutboundBytes, typeof(IAuditAggregator<double>))]
   [ManagedDataSet("inbound_dispatch_latency", DataSetNames.kInboundProcessDispatchLatency, typeof(IAuditAggregator<double>))]
   [ManagedDataSet("total_resends", DataSetNames.kTotalResends, typeof(IAuditCounter))]
   [ManagedDataSet("message_resends", DataSetNames.kMessageResends, typeof(IAuditAggregator<int>))]
   [ManagedDataSet("tossed", DataSetNames.kTossed, typeof(IAuditCounter))]
   [ManagedDataSet("duplicates_received", DataSetNames.kDuplicatesReceived, typeof(IAuditCounter))]
   [ManagedDataSet("announcements_received", DataSetNames.kAnnouncementsReceived, typeof(IAuditCounter))]
   [ManagedDataSet("multipart_chunks_sent", DataSetNames.kMultiPartChunksSent, typeof(IAuditCounter))]
   [ManagedDataSet("multipart_chunk_bytes_received", DataSetNames.kMultiPartChunksBytesReceived, typeof(IAuditAggregator<int>))]
   [ManagedDataSet("outbound_message_rate_limit", DataSetNames.kOutboundMessageRateLimit, typeof(IAuditAggregator<double>))]
   [ManagedDataSet("send_queue_depth", DataSetNames.kSendQueueDepth, typeof(IAuditAggregator<double>))]
   [Guid("CA65B41E-DB31-4EDE-81BF-525BED6747A1")]
   public class UdpDebugMob {

   }
}
