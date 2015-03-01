using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Courier.Identities;
using Dargon.Courier.Messaging;
using ItzWarty;
using ItzWarty.Collections;

namespace Dargon.Courier {
   public interface OutboundEnvelopeManager {
      Guid AddEnvelope<TMessage>(ReadableCourierEndpoint endpoint, TMessage message, MessagePriority priority);
   }

   public class OutboundEnvelopeManagerImpl : OutboundEnvelopeManager {
      private readonly GuidProxy guidProxy;
      private readonly OutboundEnvelopeContextFactory outboundEnvelopeContextFactory;
      private readonly IConcurrentDictionary<Guid, OutboundEnvelopeContext> activeEnvelopeContextsById;

      public OutboundEnvelopeManagerImpl(GuidProxy guidProxy, OutboundEnvelopeContextFactory outboundEnvelopeContextFactory)
         : this(guidProxy,
                outboundEnvelopeContextFactory, 
                new ConcurrentDictionary<Guid, OutboundEnvelopeContext>()) {
      }

      public OutboundEnvelopeManagerImpl(GuidProxy guidProxy, OutboundEnvelopeContextFactory outboundEnvelopeContextFactory, IConcurrentDictionary<Guid, OutboundEnvelopeContext> activeEnvelopeContextsById) {
         this.guidProxy = guidProxy;
         this.outboundEnvelopeContextFactory = outboundEnvelopeContextFactory;
         this.activeEnvelopeContextsById = activeEnvelopeContextsById;
      }

      public Guid AddEnvelope<TMessage>(ReadableCourierEndpoint endpoint, TMessage message, MessagePriority priority) {
         var id = guidProxy.NewGuid();
         var context = outboundEnvelopeContextFactory.Create(id, endpoint, message, priority);
         activeEnvelopeContextsById.Add(id, context);
         return id;
      }
   }
}
