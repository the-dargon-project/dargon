using Dargon.Commons;
using Dargon.Courier.TransitTier;
using System.IO;
using System.Threading.Tasks;
using Dargon.Commons.Collections;

namespace Dargon.Courier.TestUtilities {
   public class TestTransport : ITransport {
      private readonly ConcurrentSet<IAsyncPoster<InboundDataEvent>> inboundDataEventPosters = new ConcurrentSet<IAsyncPoster<InboundDataEvent>>();

      public void Start(IAsyncPoster<InboundDataEvent> inboundDataEventPoster, IAsyncSubscriber<MemoryStream> outboundDataSubscriber) {
         inboundDataEventPosters.TryAdd(inboundDataEventPoster);

         outboundDataSubscriber.Subscribe(async (s, ms) => {
            await Task.Yield();

            foreach (var poster in inboundDataEventPosters) {
               poster.PostAsync(
                  new InboundDataEvent {
                     Data = ms.ToArray()
                  }).Forget();
            }
         });
      }

      /// <summary>
      /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
      /// </summary>
      public void Dispose() { }
   }
}
