using Dargon.Commons;
using Dargon.Courier.TransitTier;
using System.IO;
using System.Threading.Tasks;

namespace Dargon.Courier.TestUtilities {
   public class TestTransport : ITransport {
      public void Start(IAsyncPoster<InboundDataEvent> inboundDataEventPoster, IAsyncSubscriber<MemoryStream> outboundDataSubscriber) {
         outboundDataSubscriber.Subscribe(async (s, ms) => {
            await Task.Yield();

            inboundDataEventPoster.PostAsync(
               new InboundDataEvent {
                  Data = ms.ToArray()
               }).Forget();
         });
      }

      /// <summary>
      /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
      /// </summary>
      public void Dispose() { }
   }
}
