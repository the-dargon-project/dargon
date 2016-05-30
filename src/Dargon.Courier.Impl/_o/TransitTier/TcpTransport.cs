using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Dargon.Courier.TransitTier {
   public class TcpTransport : ITransport {
      public void Dispose() {
         throw new NotImplementedException();
      }

      public void Start(IAsyncPoster<InboundDataEvent> inboundDataEventPoster, IAsyncSubscriber<MemoryStream> outboundDataSubscriber) {
         throw new NotImplementedException();
      }
   }
}
