using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Dargon.Courier.PubSubTier {
   [Guid("3A73BFB8-9440-4179-AF26-D6690A066B15")]
   public interface IPubSubService {
      Task SubscribeAsync(Guid topicId);
      Task UnsubscribeAsync(Guid topicId);
   }
}