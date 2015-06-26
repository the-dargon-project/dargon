using System;

namespace Dargon.Nest.Repl {
   public class CommandNotFoundException : Exception {
      public CommandNotFoundException(string s) : base(s) {
      }
   }
}
