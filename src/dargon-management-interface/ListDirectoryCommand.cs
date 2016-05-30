using Dargon.Commons;
using Dargon.Repl;
using System;

namespace Dargon.Courier.Management.UI {
   public class ListDirectoryCommand : ICommand {
      public string Name => "ls";

      public int Eval(string args) {
         string name;
         args = Tokenizer.Next(args, out name);

         if (ReplGlobals.Current == null) {
            ReplGlobals.Current = ReplGlobals.Root;
         }

         if (ReplGlobals.Root == null) {
            throw new Exception("Need to fetch mobs first");
         }

         PrettyPrint.List(
            ReplGlobals.Current.Children,
            new PrettyFormatter<SomeNode> {
            });

         return 0;
      }
   }
}
