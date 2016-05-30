using System;
using System.IO;
using System.Threading.Tasks;

namespace Dargon.Courier.TransitTier {
   public interface ITransport : IDisposable {
      void Start(IAsyncPoster<InboundDataEvent> inboundDataEventPoster, IAsyncSubscriber<MemoryStream> outboundDataSubscriber);
   }
}