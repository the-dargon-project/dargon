using System;
using Dargon.Commons;
using Dargon.Repl;

namespace Dargon.Courier.Management.Repl {
   public class TreeCommand : ICommand {
      public string Name => "tree";

      public int Eval(string args) {
         if (ReplGlobals.Current == null) {
            ReplGlobals.Current = ReplGlobals.Root;
         }

         if (ReplGlobals.Root == null) {
            throw new Exception("Need to fetch mobs first");
         }

         DumpHelper(ReplGlobals.Current, 0);
         return 0;
      }

      private void DumpHelper(SomeNode node, int indentation) {
         var indentationString = " ".Repeat(indentation);
         Console.WriteLine($"{indentationString}* {node}");
         foreach (var child in node.Children) {
            DumpHelper(child, indentation + 1);
         }
      }
   }
}
