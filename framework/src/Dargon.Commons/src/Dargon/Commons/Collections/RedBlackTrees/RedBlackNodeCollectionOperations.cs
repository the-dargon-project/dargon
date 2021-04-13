using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            sb.AppendLine($"  {nid} [label=\"h={n.BlackHeight}, v={n.Value}\" color=\"{color}\"]");

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
