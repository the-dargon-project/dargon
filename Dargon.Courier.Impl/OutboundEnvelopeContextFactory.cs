using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Courier.Identities;
using Dargon.Courier.Messaging;

namespace Dargon.Courier {
   public interface OutboundEnvelopeContextFactory {
      OutboundEnvelopeContext<TMessage> Create<TMessage>(Guid id, ReadableCourierEndpoint endpoint, TMessage message, MessagePriority priority);
   }

   public class OutboundEnvelopeContextFactoryImpl : OutboundEnvelopeContextFactory {
      public OutboundEnvelopeContext<TMessage> Create<TMessage>(Guid id, ReadableCourierEndpoint endpoint, TMessage message, MessagePriority priority) {
         return new OutboundEnvelopeContextImpl<TMessage>(id, endpoint, message, priority);
      }
   }
}
