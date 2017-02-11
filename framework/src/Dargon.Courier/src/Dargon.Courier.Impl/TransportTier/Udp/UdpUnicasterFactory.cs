using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dargon.Commons.Pooling;
using Dargon.Courier.AuditingTier;

namespace Dargon.Courier.TransportTier.Udp {
   public interface IUdpUnicasterFactory {
      UdpUnicaster Create(UdpClientRemoteInfo remoteInfo);
   }

   public class UdpUnicasterFactory : IUdpUnicasterFactory {
      private readonly Identity identity;
      private readonly UdpClient udpClient;
      private readonly AcknowledgementCoordinator acknowledgementCoordinator;
      private readonly IObjectPool<byte[]> sendReceiveBufferPool;
      private readonly IAuditCounter resendsCounter;
      private readonly IAuditAggregator<int> resendsAggregator;
      private readonly IAuditAggregator<double> outboundMessageRateLimitAggregator;
      private readonly IAuditAggregator<double> sendQueueDepthAggregator;

      public UdpUnicasterFactory(Identity identity, UdpClient udpClient, AcknowledgementCoordinator acknowledgementCoordinator, IObjectPool<byte[]> sendReceiveBufferPool, IAuditCounter resendsCounter, IAuditAggregator<int> resendsAggregator, IAuditAggregator<double> outboundMessageRateLimitAggregator, IAuditAggregator<double> sendQueueDepthAggregator) {
         this.identity = identity;
         this.udpClient = udpClient;
         this.acknowledgementCoordinator = acknowledgementCoordinator;
         this.sendReceiveBufferPool = sendReceiveBufferPool;
         this.resendsCounter = resendsCounter;
         this.resendsAggregator = resendsAggregator;
         this.outboundMessageRateLimitAggregator = outboundMessageRateLimitAggregator;
         this.sendQueueDepthAggregator = sendQueueDepthAggregator;
      }

      public UdpUnicaster Create(UdpClientRemoteInfo remoteInfo) {
         return new UdpUnicaster(identity, udpClient, acknowledgementCoordinator, sendReceiveBufferPool, remoteInfo, resendsCounter, resendsAggregator, outboundMessageRateLimitAggregator, sendQueueDepthAggregator);
      }
   }
}
