using System;
using Dargon.Courier.Identities;
using Dargon.Courier.Messaging;

namespace Dargon.Courier {
   public class CourierClientImpl : CourierClient {
      public Guid SendReliableUnicast<TMessage>(
         ReadableCourierEndpoint endpoint, 
         TMessage message, 
         MessagePriority priority
      ) {
         throw new NotImplementedException();
      }
   }
}
