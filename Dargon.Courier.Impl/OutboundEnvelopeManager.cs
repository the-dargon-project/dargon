using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dargon.Courier {
   public interface OutboundEnvelopeManager {
      Guid AddEnvelope<TMessage>(CourierEndpoint endpoint, TMessage message, MessagePriority priority);
   }

   public class OutboundEnvelopeManagerImpl : OutboundEnvelopeManager {
      public Guid AddEnvelope<TMessage>(CourierEndpoint endpoint, TMessage message, MessagePriority priority) {
         throw new NotImplementedException();
      }
   }
}
