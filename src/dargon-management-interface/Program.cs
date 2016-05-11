using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dargon.Repl;

namespace Dargon.Courier.Management.UI {
   public class Program {
      public static void Main() {
         var dispatcher = new DispatcherCommand("root");
         dispatcher.RegisterCommand(new ExitCommand());
         new ReplCore(dispatcher).Run();
      }
   }
}
