using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Commons.Cli;

namespace Dargon.Commons.Collections.RedBlackTrees {
   /// <summary>
   /// Variation of MIT-licensed SortedSet from .NET Foundation
   ///
   /// Implements Split/Join as described in:
   ///    Guy E. Blelloch, Daniel Ferizovic, and Yihan Sun. 2016.
   ///    Just Join for Parallel Ordered Sets. In Proceedings of the 28th ACM Symposium
   ///    on Parallelism in Algorithms and Architectures (SPAA '16). Association for
   ///    Computing Machinery, New York, NY, USA, 253–264. DOI:https://doi.org/10.1145/2935764.2935768
   /// </summary>
   public partial class RedBlackNodeCollectionOperations<T, TComparer> where TComparer : struct, IComparer<T> {
      private readonly TComparer comparer;

      public RedBlackNodeCollectionOperations(TComparer comparer) {
         this.comparer = comparer;
      }

      public RedBlackNode<T> CreateEmptyTree() => null;

      public string ToFlatStringDebug(RedBlackNode<T> n) {
         if (n == null) return "[null]";

         return $"[{n.Value} / {n.Color}, L={ToFlatStringDebug(n.Left)}, R={ToFlatStringDebug(n.Right)}]";
      }

      //  10
      //  ├── L: 5
      //  │   ├── L: 0
      //  │   └── R: 7
      //  └── R: 20
      public void DumpToConsoleColored(RedBlackNode<T> initialNode) {
         var s = new Stack<(RedBlackNode<T> node, string p1, string p2, string p3, string p4, string p5)>();
         //                   p1        p2        p3     p4        p5
         var lstr  = "├── L:";
         var lpadd = "│   ";
         var rstr  = "└── R:";
         var rpadd = "    ";
         s.Push((initialNode, "Root:", lstr, lpadd, rstr, rpadd));
         while (s.Count > 0) {
            var (n, p1, p2, p3, p4, p5) = s.Pop();
            if (n == null) {
               using (new ConsoleColorSwitch().To(ConsoleColor.DarkGray)) {
                  Console.Write($"{p1} null");
               }
               Console.WriteLine();

               continue;
            } else {
               Console.Write(p1 + " ");
               var (fg, bg) = n.Color == RedBlackColor.Red
                  ? (ConsoleColor.Cyan, ConsoleColor.Red)
                  : (ConsoleColor.Black, ConsoleColor.DarkGray);
               using (new ConsoleColorSwitch().To(fg, bg)) {
                  Console.Write(n.Color);
               }

               var verified = true;
               try {
                  using (Assert.OpenThreadOutputSuppressionBlock()) {
                     VerifyInvariants(n, false);
                  }
               } catch {
                  verified = false;
               }

               Console.Write($" {n.Value}, bh={n.BlackHeight} pval={n.Parent?.Value.ToString() ?? "N/A"} ");
               using (new ConsoleColorSwitch().To(verified ? ConsoleColor.Green : ConsoleColor.Red)) {
                  Console.Write(verified ? "[OK]" : "[INVALID]");
               }
               Console.WriteLine();
            }

            s.Push((n.Right, p4, p5 + lstr, p5 + lpadd, p5 + rstr, p5 + rpadd));
            s.Push((n.Left, p2, p3 + lstr, p3 + lpadd, p3 + rstr, p3 + rpadd));
         }
      }

      public string ToGraphvizStringDebug(RedBlackNode<T> initialNode) {
         var sb = new StringBuilder();
         sb.AppendLine("digraph RedBlackTree {");

         var nextNodeId = 0;

         var s = new Stack<(RedBlackNode<T> node, int nodeId, RedBlackNode<T> pred, int predId)>();
         if (initialNode != null) {
            s.Push((initialNode, nextNodeId++, null, -1));
         }

         while (s.Count > 0) {
            var (n, nid, p, pid) = s.Pop();
            var color = n.IsRed ? "red" : "black";
            sb.AppendLine($"  {nid} [label=\"h={n.BlackHeight}, v={n.Value}, (pval={n.Parent?.Value.ToString() ?? "N/A"})\" color=\"{color}\"]");

            if (pid >= 0) {
               sb.AppendLine($"  {pid} -> {nid}");
            }

            if (n.Left != null) s.Push((n.Left, nextNodeId++, n, nid));
            if (n.Right != null) s.Push((n.Right, nextNodeId++, n, nid));
         }

         sb.AppendLine("}");
         return sb.ToString();
      }
   }
}
