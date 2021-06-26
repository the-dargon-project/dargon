using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Commons.Templating;

namespace Dargon.Commons.Collections.RedBlackTrees {
   public partial class RedBlackNodeCollectionOperations<T> {
      public int ComputeTreeifyRootBlackHeight(int length) {
         // Assumes all levels of the tree are fully black except the bottommost level.
         // Unless we can have a perfect binary tree, then Treeify makes all levels black.
         // 0 nodes => 0 * (null child has bh 0)
         // 1 nodes => 1 * (null children count as blacks)
         // 2 nodes => 1
         // 3 nodes => 2 *
         // 4 nodes => 2
         // 5 nodes => 2
         // 6 nodes => 2
         // 7 nodes => 3 *
         if (length == 0) return 0;

         var nPlus1 = length + 1;

         // technically our answer is just floor(log2(n + 1))
         // I'm a bit afraid of floating point error messing up the calculations,
         // so I've added an additional epsilon
         return (int)Math.Log2(nPlus1 + 1E-5);
      }

      public RedBlackNode<T> Treeify(Span<T> values) {
         return TreeifyValuesInternalOuter<TFalse>(values, Span<RedBlackNode<T>>.Empty);
      }

      public RedBlackNode<T> Treeify(Span<T> values, Span<RedBlackNode<T>> indexToNode) {
         return TreeifyValuesInternalOuter<TTrue>(values, indexToNode);
      }

      private RedBlackNode<T> TreeifyValuesInternalOuter<TBoolEnableIndexToNodeOpt>(Span<T> values, Span<RedBlackNode<T>> indexToNodeOpt) {
         if (values.Length == 0) {
            return null;
         }

         // (assume depth of 1 node is 0)
         // 1 node  -> 0 // perfect tree
         // 2 nodes -> 1 // bottom row is red
         // 3 nodes -> 0 // perfect tree
         // 4 nodes -> 2 // bottom row is red
         // 5 nodes -> 2 // bottom row is red
         // 6 nodes -> 2 // bottom row is red
         // 7 nodes -> 0 // perfect tree
         var nPlus1 = values.Length + 1;
         var nPlus1IsPowerOf2 = (nPlus1 & (nPlus1 - 1)) == 0;

         var redDepth =
            nPlus1IsPowerOf2
               ? -1
               : (int)Math.Floor(Math.Log2(values.Length) + 1E-5 /* Epsilon maybe unnecessary */);

         return TreeifyValuesInternalRecursive<TBoolEnableIndexToNodeOpt>(values, 0, values.Length - 1, 0, redDepth, indexToNodeOpt);
      }

      private RedBlackNode<T> TreeifyValuesInternalRecursive<TBoolEnableIndexToNodeOpt>(Span<T> values, int lo, int hi, int depth, int redDepth, Span<RedBlackNode<T>> indexToNodeOpt) {
         if (lo > hi) {
            return null;
         }

         var mid = (lo + hi) / 2;
         var left = TreeifyValuesInternalRecursive<TBoolEnableIndexToNodeOpt>(values, lo, mid - 1, depth + 1, redDepth, indexToNodeOpt);
         var right = TreeifyValuesInternalRecursive<TBoolEnableIndexToNodeOpt>(values, mid + 1, hi, depth + 1, redDepth, indexToNodeOpt);

         var node = new RedBlackNode<T>(
            values[mid],
            depth == redDepth ? RedBlackColor.Red : RedBlackColor.Black,
            left,
            right,
            BlackHeightUtils.ComputeForParent(left));

         if (TBool.IsTrue<TBoolEnableIndexToNodeOpt>()) {
            indexToNodeOpt[mid] = node;
         }

         return node;
      }

      public RedBlackNode<T> Treeify(Span<RedBlackNode<T>> nodes) {
         return TreeifyNodesInternalOuter(nodes);
      }

      private RedBlackNode<T> TreeifyNodesInternalOuter(Span<RedBlackNode<T>> nodes) {
         if (nodes.Length == 0) {
            return null;
         }

         // (assume depth of 1 node is 0)
         // 1 node  -> 0 // perfect tree
         // 2 nodes -> 1 // bottom row is red
         // 3 nodes -> 0 // perfect tree
         // 4 nodes -> 2 // bottom row is red
         // 5 nodes -> 2 // bottom row is red
         // 6 nodes -> 2 // bottom row is red
         // 7 nodes -> 0 // perfect tree
         var nPlus1 = nodes.Length + 1;
         var nPlus1IsPowerOf2 = (nPlus1 & (nPlus1 - 1)) == 0;

         var redDepth =
            nPlus1IsPowerOf2
               ? -1
               : (int)Math.Floor(Math.Log2(nodes.Length) + 1E-5 /* Epsilon maybe unnecessary */);

         return TreeifyNodesInternalRecursive(nodes, 0, nodes.Length - 1, 0, redDepth);
      }

      private RedBlackNode<T> TreeifyNodesInternalRecursive(Span<RedBlackNode<T>> nodes, int lo, int hi, int depth, int redDepth) {
         if (lo > hi) {
            return null;
         }

         var mid = (lo + hi) / 2;
         var left = TreeifyNodesInternalRecursive(nodes, lo, mid - 1, depth + 1, redDepth);
         var right = TreeifyNodesInternalRecursive(nodes, mid + 1, hi, depth + 1, redDepth);

         var node = nodes[mid];

         if (depth == 0) {
            node.Parent = null;
         }

         node.Left = left;
         if (left != null) left.Parent = node;
         
         node.Right = right;
         if (right != null) right.Parent = node;
         
         node.Color = depth == redDepth ? RedBlackColor.Red : RedBlackColor.Black;
         node.BlackHeight = BlackHeightUtils.ComputeForParent(left);

         return node;
      }
   }
}
