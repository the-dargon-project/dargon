using System.Collections.Generic;
using Dargon.Repl;

namespace Dargon.Courier.Management.Repl {
   public class DispatcherMultiCommand : ICommand {
      private readonly string name;
      private readonly IEnumerable<string> commands;
      private readonly IDispatcher dispatcher;

      public string Name => name;

      public DispatcherMultiCommand(string name, IEnumerable<string> commands, IDispatcher dispatcher) {
         this.name = name;
         this.commands = commands;
         this.dispatcher = dispatcher;
      }

      public int Eval(string args) {
         foreach (var s in commands) {
            dispatcher.Eval(s);
         }
         return 0;
      }
   }
}
