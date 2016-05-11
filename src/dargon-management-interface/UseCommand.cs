using Dargon.Repl;
using Dargon.Ryu;

namespace Dargon.Courier.Management.UI {
   public class UseCommand : DispatcherCommand {
      public UseCommand() : base("use") {
         var courierUdp = new CourierUdpClusterCommand();
         RegisterCommand(courierUdp);
         RegisterCommand(new AliasCommand("udp", courierUdp));
      }

      public class CourierUdpClusterCommand : ICommand {
         public string Name => "courier-udp";

         public int Eval(string args) {
            string endpoint = string.IsNullOrWhiteSpace(args) ? "235.13.33.37:21337" : args.Trim();
            var courier = new CourierContainerFactory(new RyuFactory().Create()).Create();

         }
      }
   }
}
