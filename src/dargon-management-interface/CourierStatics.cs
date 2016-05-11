using Dargon.Courier.PeeringTier;
using Dargon.Ryu;

namespace Dargon.Courier.Management.UI {
   public static class CourierStatics {
      public static void Initialize() {
         var courier = new CourierContainerFactory(new RyuFactory().Create()).Create();
         PeerTable = courier.GetOrThrow<PeerTable>();
      }

      public static PeerTable PeerTable { get; private set; }
   }
}
