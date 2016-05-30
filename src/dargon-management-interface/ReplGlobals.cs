using Dargon.Courier.ManagementTier;
using Dargon.Courier.PeeringTier;
using Dargon.Ryu;

namespace Dargon.Courier.Management.UI {
   public static class ReplGlobals {
      public static CourierFacade CourierFacade { get; set; }
      public static IManagementObjectService ManagementObjectService { get; set; }
      public static SomeNode Root { get; set; }
      public static SomeNode Current { get; set; }
   }
}
