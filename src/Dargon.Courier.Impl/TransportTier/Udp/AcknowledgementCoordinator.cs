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
            Interlocked.Increment(ref DebugRuntimeStats.out_rs_acked);
            signal.Set();
         }
      }

      public class Signal {
         private readonly object sync = new object();
         private bool isSet;

         public void Set() {
            lock (sync) {
               isSet = true;
            }
         }

         public bool IsSet() {
            lock (sync) {
               return isSet;
            }
         }
      }
   }
}