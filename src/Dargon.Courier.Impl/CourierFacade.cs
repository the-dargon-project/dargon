using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dargon.Commons.Collections;
using Dargon.Courier.TransportTier;

namespace Dargon.Courier {
   public class CourierFacade {
      private readonly IConcurrentSet<ITransport> transports;

      public CourierFacade(IConcurrentSet<ITransport> transports) {
         this.transports = transports;
      }

      public async Task ShutdownAsync() {
         foreach (var transport in transports) {
            await transport.ShutdownAsync();
         }
      }
   }
}
