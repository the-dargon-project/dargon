using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Repl;

namespace Dargon.Courier.Management.UI {
   public class ChangeDirectoryCommand : ICommand {
      public string Name => "cd";

      public int Eval(string args) {
         string path;
         args = Tokenizer.Next(args, out path);

         if (ReplGlobals.Current == null) {
            ReplGlobals.Current = ReplGlobals.Root;
         }

         if (ReplGlobals.Root == null) {
            throw new Exception("Need to fetch mobs first");
         }

         if (path.StartsWith("!!")) {
            var nodeName = path.Substring(2);
            var node = ReplGlobals.Root.RecursivelyDescend(
               new List<SomeNode>(),
               AccumulatorInclusion.Include,
               AccumulatorInclusion.Include,
               x => x.Children.Count == 0,
               x => x.Children).First(x => x.Item1.Name.Equals(nodeName, StringComparison.OrdinalIgnoreCase));
            ReplGlobals.Current = node.Item1;
            return 0;
         }

         var breadcrumbs = path.Split("/");
         foreach (var breadcrumb in breadcrumbs) {
            if (string.IsNullOrWhiteSpace(breadcrumb)) {
               ReplGlobals.Current = ReplGlobals.Root;
            } else if (breadcrumb == "..") {
               ReplGlobals.Current = ReplGlobals.Current?.Parent;
            } else {
               SomeNode child;
               if (ReplGlobals.Current.TryGetChild(breadcrumb, out child)) {
                  ReplGlobals.Current = child;
               } else {
                  Console.Error.WriteLine($"Could not find directory {breadcrumb}.");
                  return 1;
               }
            }
         }
         return 0;
      }
   }
}
