using Dargon.Courier.AuditingTier;
using System;

namespace Dargon.Courier.TransportTier.Udp {
   public class OutboundMessageRateLimiter {
      private readonly IAuditAggregator<double> outboundMessageRateLimitAggregator;

      private readonly int baseRate;
      private readonly int rateVelocity;
      private int currentRate;
      private readonly double packetLossDecayFactor;
      private DateTime lastIterationTime;

      public OutboundMessageRateLimiter(IAuditAggregator<double> outboundMessageRateLimitAggregator, int baseRate, int rateVelocity, int initialRate, double packetLossDecayFactor) {
         this.outboundMessageRateLimitAggregator = outboundMessageRateLimitAggregator;
         this.baseRate = baseRate;
         this.rateVelocity = rateVelocity;
         this.currentRate = initialRate;
         this.packetLossDecayFactor = packetLossDecayFactor;
         this.lastIterationTime = DateTime.Now;
      }

      public void HandlePacketLoss() {
         currentRate = baseRate + (int)((currentRate - baseRate) * packetLossDecayFactor);
      }

      public int TakeAndResetOutboundMessagesAvailableCounter() {
         // Compute time interval represented by this iteration.
         var now = DateTime.Now;
         var dt = (now - lastIterationTime).TotalSeconds;
         lastIterationTime = now;

         // Update current rate based on dt and rate velocity.
         var deltaMessageRate = (int)(dt * rateVelocity);
         currentRate += deltaMessageRate;
         outboundMessageRateLimitAggregator.Put(currentRate);

         // Compute outbound messages available based on message rate and iteration interval.
         return (int)(currentRate * dt);
      }
   }
}
