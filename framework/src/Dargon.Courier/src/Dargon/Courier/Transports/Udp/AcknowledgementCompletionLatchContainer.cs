using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Dargon.Commons;
using Dargon.Commons.AsyncPrimitives;
using Dargon.Courier.TransportTier.Udp.Vox;

namespace Dargon.Courier.TransportTier.Udp {
   public class AcknowledgementCompletionLatchContainer {
      private readonly ConcurrentDictionary<Guid, AsyncLatch> signalsByAckId = new();
      private readonly Identity identity;

      public AcknowledgementCompletionLatchContainer(Identity identity) {
         this.identity = identity;
      }

      public AsyncLatch Expect(Guid id) {
         var signal = new AsyncLatch();
         signalsByAckId.AddOrThrow(id, signal);
         return signal;
      }

      public void ProcessAcknowledgements(List<AcknowledgementDto> acks) {
         Interlocked.Add(ref DebugRuntimeStats.in_ack, acks.Count);
         foreach (var ack in acks) {
            if (signalsByAckId.TryRemove(ack.MessageId, out var signal)) {
               signal.SetOrThrow();
            }

            Interlocked.Increment(ref DebugRuntimeStats.in_ack_done);
         }
      }
   }
}