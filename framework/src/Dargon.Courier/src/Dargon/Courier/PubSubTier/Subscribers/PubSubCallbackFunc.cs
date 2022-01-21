using System.Threading.Tasks;
using Dargon.Courier.PubSubTier.Vox;

namespace Dargon.Courier.PubSubTier.Subscribers {
   public delegate Task PubSubCallbackFunc(PubSubNotification notification);
}