using System;

namespace Dargon.Repl {
   public class CommandNotFoundException : Exception {
      public CommandNotFoundException(string s) : base(s) {
      }
   }
}
