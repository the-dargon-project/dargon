using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Commons.Cli;

namespace Dargon.Commons.Collections.RedBlackTrees {
   public static class RedBlackNodeCollectionOperations {
      public static RedBlackNodeCollectionOperations<T> CreateFor<T>() {
         return new RedBlackNodeCollectionOperations<T>();
      }

      public static RedBlackNodeCollectionOperations<T> CreateFor<T>(RedBlackNode<T> t = null) {
         return new RedBlackNodeCollectionOperations<T>();
      }
   }

   public partial class RedBlackNodeCollectionOperations<T> {
      private struct DummyComparer : IComparer<T> {
         public int Compare(T? x, T? y) => throw new NotSupportedException();
      }

      public RedBlackNode<T> CreateEmptyTree() => null;
      public string ToFlatStringDebug(RedBlackNode<T> n) {
         if (n == null) return "[null]";

         return $"[{n.Value} / {n.Color}, L={ToFlatStringDebug(n.Left)}, R={ToFlatStringDebug(n.Right)}]";
      }

      public void DumpToConsoleColoredWithoutTestingInOrderInvariants(RedBlackNode<T> initialNode) {
         var cmp = new DummyComparer();
         DumpToConsoleColored(initialNode, cmp, false);
      }

      //  10
      //  ├── L: 5
      //  │   ├── L: 0
      //  │   └── R: 7
      //  └── R: 20
      public void DumpToConsoleColored<TComparer>(RedBlackNode<T> initialNode, in TComparer comparer, bool testInOrderInvariants = true)
         where TComparer : struct, IComparer<T> {
         var s = new Stack<(RedBlackNode<T> node, string p1, string p2, string p3, string p4, string p5)>();
         //                   p1        p2        p3     p4        p5
         var lstr = "├── L:";
         var lpadd = "│   ";
         var rstr = "└── R:";
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
                  using (Assert.OpenFailureLogAndBreakpointSuppressionBlock()) {
                     VerifyInvariants(n, comparer, false, testInOrderInvariants);
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

      public RedBlackNode<T> ReplaceNode(RedBlackNode<T> root, RedBlackNode<T> inTreeReplacee, RedBlackNode<T> outOfTreeReplacement) {
         inTreeReplacee.AssertIsNotNull();
         outOfTreeReplacement.AssertIsNotNull();
         outOfTreeReplacement.BlackHeight.AssertEquals(BlackHeightUtils.ComputeForParent<int>(null));
         outOfTreeReplacement.Left.AssertIsNull();
         outOfTreeReplacement.Right.AssertIsNull();
         outOfTreeReplacement.Parent.AssertIsNull();

         outOfTreeReplacement.Color = inTreeReplacee.Color;
         outOfTreeReplacement.BlackHeight = inTreeReplacee.BlackHeight;
         inTreeReplacee.Color = RedBlackColor.Black;
         inTreeReplacee.BlackHeight = BlackHeightUtils.ComputeForParent<int>(null);

         if (inTreeReplacee.Left is { } left) {
            outOfTreeReplacement.Left = left;
            inTreeReplacee.Left = null;
            left.Parent = outOfTreeReplacement;
         }

         if (inTreeReplacee.Right is { } right) {
            outOfTreeReplacement.Right = right;
            inTreeReplacee.Right = null;
            right.Parent = outOfTreeReplacement;
         }

         if (inTreeReplacee.Parent is { } parent) {
            parent.ReplaceChild(inTreeReplacee, outOfTreeReplacement);
            inTreeReplacee.Parent = null;
            return root;
         } else {
            return outOfTreeReplacement;
         }
      }
   }

   /// <summary>
   /// Variation of MIT-licensed SortedSet from .NET Foundation
   ///
   /// Split/Join is inspired by the description in:
   ///    Guy E. Blelloch, Daniel Ferizovic, and Yihan Sun. 2016.
   ///    Just Join for Parallel Ordered Sets. In Proceedings of the 28th ACM Symposium
   ///    on Parallelism in Algorithms and Architectures (SPAA '16). Association for
   ///    Computing Machinery, New York, NY, USA, 253–264. DOI:https://doi.org/10.1145/2935764.2935768
   ///
   /// My join implementation is wildly different and derived from scratch. I'm still not sure how the
   /// paper's works, but mine should be logN as well.
   /// </summary>
   public partial class RedBlackNodeCollectionOperations<T, TComparer> : RedBlackNodeCollectionOperations<T> where TComparer : struct, IComparer<T> {
      private TComparer comparer;

      public RedBlackNodeCollectionOperations(TComparer comparer) {
         this.comparer = comparer;
      }

      public void DumpToConsoleColored(RedBlackNode<T> initialNode, bool testInvariants = true) => DumpToConsoleColored(initialNode, in comparer, testInvariants);

      public ref TComparer Comparer => ref comparer;
   }
}
