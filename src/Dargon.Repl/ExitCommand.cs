using System;

namespace Dargon.Nest.Repl {
   public class ExitCommand : ICommand {
      public string Name { get { return "exit"; } }

      public int Eval(string args) {
         Environment.Exit(0);
         return 0;
      }
   }
}