using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Courier.Identities;
using Dargon.Courier.Messaging;

namespace Dargon.Courier {
   public interface OutboundEnvelopeContext {

   }

   public interface OutboundEnvelopeContext<TMessage> : OutboundEnvelopeContext {

   }

   public class OutboundEnvelopeContextImpl<TMessage> : OutboundEnvelopeContext<TMessage> {
      private readonly Guid id;
      private readonly ReadableCourierEndpoint endpoint;
      private readonly TMessage message;
      private readonly MessagePriority priority;

      public OutboundEnvelopeContextImpl(Guid id, ReadableCourierEndpoint endpoint, TMessage message, MessagePriority priority) {
         this.id = id;
         this.endpoint = endpoint;
         this.message = message;
         this.priority = priority;
      }

      public Guid Id { get { return id; } }
      public ReadableCourierEndpoint Endpoint { get { return endpoint; } }
      public TMessage Message { get { return message; } }
      public MessagePriority Priority { get { return priority; } }
   }
}
