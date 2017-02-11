using Dargon.Commons.Collections;
using Dargon.Courier.TransportTier.Udp.Vox;
using System;
using System.Threading;

namespace Dargon.Courier.TransportTier.Udp {
   public class AcknowledgementCoordinator {
      private readonly ConcurrentDictionary<Guid, Signal> signalsByAckId = new ConcurrentDictionary<Guid, Signal>();
      private readonly Identity identity;

      public AcknowledgementCoordinator(Identity identity) {
         this.identity = identity;
      }

      public Signal Expect(Guid id) {
         var signal = new Signal();
         signalsByAckId.AddOrThrow(id, signal);
         return signal;
      }

      public void ProcessAcknowledgement(AcknowledgementDto ack) {
         Signal signal;
         if (signalsByAckId.TryRemove(ack.MessageId, out signal)) {
#if DEBUG
            Interlocked.Increment(ref DebugRuntimeStats.out_rs_acked);
#endif
            signal.Set();
         }
      }

      public class Signal {
         private const int kStateUnset = 0;
         private const int kStateSet = 1;
         private int state = kStateUnset;

         public void Set() {
            Interlocked.CompareExchange(ref state, kStateSet, kStateUnset);
         }

         public bool IsSet() {
            return Interlocked.CompareExchange(ref state, kStateUnset, kStateUnset) == kStateSet;
         }
      }
   }
}