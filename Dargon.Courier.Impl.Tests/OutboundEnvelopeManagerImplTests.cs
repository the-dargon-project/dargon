using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Courier.Identities;
using Dargon.Courier.Impl.Tests.Helpers;
using Dargon.Courier.Messaging;
using Dargon.PortableObjects;
using ItzWarty;
using ItzWarty.Collections;
using NMockito;
using Xunit;

namespace Dargon.Courier.Impl.Tests {
   public class OutboundEnvelopeManagerImplTests : NMockitoInstance {
      [Mock] private readonly GuidProxy guidProxy = null;
      [Mock] private readonly IConcurrentDictionary<Guid, OutboundEnvelopeContext> activeEnvelopeContextsById = null;
      [Mock] private readonly OutboundEnvelopeContextFactory outboundEnvelopeContextFactory = null;

      private readonly Guid kMessageId = Guid.NewGuid();
      private readonly MessagePriority kMessagePriority = MessagePriority.High;

      private readonly OutboundEnvelopeManager testObj;

      public OutboundEnvelopeManagerImplTests() {
         testObj = new OutboundEnvelopeManagerImpl(guidProxy, outboundEnvelopeContextFactory, activeEnvelopeContextsById);
      }

      [Fact]
      public void AddEnvelopeCreatesAndStoresEnvelopeContextTest() {
         var endpoint = CreateMock<ReadableCourierEndpoint>();
         var message = CreateMock<DummyMessage>();
         var outboundEnvelopeContext = CreateMock<OutboundEnvelopeContext<DummyMessage>>();

         When(guidProxy.NewGuid()).ThenReturn(kMessageId);
         When(outboundEnvelopeContextFactory.Create(kMessageId, endpoint, message, kMessagePriority)).ThenReturn(outboundEnvelopeContext);

         testObj.AddEnvelope(endpoint, message, kMessagePriority);

         Verify(guidProxy).NewGuid();
         Verify(outboundEnvelopeContextFactory).Create(kMessageId, endpoint, message, kMessagePriority);
         Verify(activeEnvelopeContextsById).Add(kMessageId, outboundEnvelopeContext);
         VerifyNoMoreInteractions();
      }
   }
}
