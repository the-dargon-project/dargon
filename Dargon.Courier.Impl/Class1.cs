using System;

namespace Dargon.Courier {
   public class CourierClientImpl : CourierClient {
      public Guid SendReliableUnicast<TMessage>(CourierEndpoint endpoint, TMessage message, MessagePriority priority) {
         throw new NotImplementedException();
      }
   }
}
