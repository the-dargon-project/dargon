using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dargon.Courier {
   public interface CourierClient {
      Guid SendReliableUnicast<TMessage>(CourierEndpoint endpoint, TMessage message, MessagePriority priority);
   }
}
