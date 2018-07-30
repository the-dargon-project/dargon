using Dargon.Courier.ManagementTier;

namespace Dargon.Courier.Management.Repl {
   public static class ReplGlobals {
      public static CourierFacade CourierFacade { get; set; }
      public static IManagementObjectService ManagementObjectService { get; set; }
      public static SomeNode Root { get; set; }
      public static SomeNode Current { get; set; }
   }
}
