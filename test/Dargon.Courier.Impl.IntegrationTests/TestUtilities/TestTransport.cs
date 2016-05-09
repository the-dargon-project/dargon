using Dargon.Commons;
using Dargon.Courier.TransitTier;
using System.IO;
using System.Threading.Tasks;

namespace Dargon.Courier.TestUtilities {
   public class TestTransport : ITransport {
      private IAsyncPoster<InboundDataEvent> inboundDataEventPoster;
      private IAsyncSubscriber<MemoryStream> outboundDataSubscriber;

      public void Start(IAsyncPoster<InboundDataEvent> inboundDataEventPoster, IAsyncSubscriber<MemoryStream> outboundDataSubscriber) {
         this.inboundDataEventPoster = inboundDataEventPoster;
         this.outboundDataSubscriber = outboundDataSubscriber;

         this.outboundDataSubscriber.Subscribe(HandleOutboundData);
      }

      private Task HandleOutboundData(IAsyncSubscriber<MemoryStream> s, MemoryStream ms) {
         inboundDataEventPoster.PostAsync(
            new InboundDataEvent {
               Data = ms.ToArray()
            }).Forget();
         return Task.FromResult(false);
      }

      /// <summary>
      /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
      /// </summary>
      public void Dispose() { }
   }
}
