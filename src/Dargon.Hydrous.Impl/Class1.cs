using System.Threading.Tasks;
using Dargon.Courier;
using Dargon.Courier.AsyncPrimitives;

namespace Dargon.Hydrous.Impl {
   public class X {
      private readonly InboundMessageRouter router;
      private readonly Messenger messenger;

      public X(InboundMessageRouter router, Messenger messenger) {
         this.router = router;
         this.messenger = messenger;
      }

      public void Initialize() {
      }

      public async Task RunAsync() {
         var isCoordinator = await JoinClusterAsync();
         if (isCoordinator) {

         }
         while (true) {

         }
      }

      public async Task<bool> JoinClusterAsync() {
         return false;
      }
   }
}
