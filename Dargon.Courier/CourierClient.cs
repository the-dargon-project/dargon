using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Courier.Identities;
using Dargon.Courier.Messaging;

namespace Dargon.Courier {
   public interface CourierClient {
      Guid SendReliableUnicast<TMessage>(ReadableCourierEndpoint endpoint, TMessage message, MessagePriority priority);
   }
}
