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
               GetBackground = n => {
                  if (n.PropertyDto != null) {
                     if (n.PropertyDto.HasGetter && n.PropertyDto.HasSetter) {
                        return ConsoleColor.Magenta;
                     } else if (n.PropertyDto.HasGetter) {
                        return ConsoleColor.Cyan;
                     } else if (n.PropertyDto.HasSetter) {
                        return ConsoleColor.Red;
                     }
                  }
                  return ConsoleColor.Black;
               }
            });

         return 0;
      }
   }
}
