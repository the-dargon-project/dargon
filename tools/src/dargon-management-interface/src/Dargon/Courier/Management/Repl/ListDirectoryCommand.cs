using System;
using Dargon.Commons;
using Dargon.Repl;

namespace Dargon.Courier.Management.Repl {
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
                  var color = ConsoleColor.Black;
                  if (n.PropertyDto != null) {
                     if (n.PropertyDto.HasGetter && n.PropertyDto.HasSetter) {
                        color = ConsoleColor.DarkMagenta;
                     } else if (n.PropertyDto.HasGetter) {
                        color = ConsoleColor.DarkCyan;
                     } else if (n.PropertyDto.HasSetter) {
                        color = ConsoleColor.DarkRed;
                     }
                  }
                  if (n.DataSetDto != null) {
                     color |= (ConsoleColor)8;
                  }
                  return color;
               }
            });

         return 0;
      }
   }
}
