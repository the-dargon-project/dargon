using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dargon.Commons.Collections.RedBlackTrees {
   public partial class RedBlackNodeCollectionOperations<T, TComparer> where TComparer : struct, IComparer<T> {
      /// <summary>
      /// Disables runtime checks on invariants that should always pass
      /// </summary>
      private const bool kEnableDebugVerifyInvariants = false;

      public void VerifyInvariants(RedBlackNode<T> initialNode, bool assertTreeRootInvariants = true) {
         // Invariants:
         // (1) Each node is either red or black. - A given with our implementation
         // (2) All NIL leaves(figure 1) are considered black (Implicit NIL leaves with our implementation - nothing to compute)
         // (3) If a node is red, then both its children are black.
         // (4) Every path from a given node to any of its descendant NIL leaves goes through the same number of black nodes.
         // 
         // Additionally (for join/split support):
         // (5) Every node tracks its black height, the number of black nodes to its leaves
         if (initialNode == null) return;

         if (assertTreeRootInvariants) {
            initialNode.Color.AssertEquals(RedBlackColor.Black);
         }

         // rootToNode<T>Blacks: number of black nodes in [root, ..., node]
         var q = new Queue<(RedBlackNode<T> n, int rootToNodeBlacks, RedBlackNode<T> parent)>();
         q.Enqueue((initialNode, initialNode.IsBlack ? 1 : 0, null));

         var leaves = new List<RedBlackNode<T>>();
         var nodeToRootToNodeBlacks = new Dictionary<RedBlackNode<T>, int>();

         while (q.Count > 0) {
            var (n, rootToNodeBlacks, parentOrNull) = q.Dequeue();
            nodeToRootToNodeBlacks[n] = rootToNodeBlacks;

            if (assertTreeRootInvariants || n != initialNode) {
               Assert.Equals(parentOrNull, n.Parent);

               if (parentOrNull is { } parent) {
                  // If a node is red, then both its children are black.
                  // (alternatively, no red node has a red parent)
                  Assert.IsFalse(n.IsRed && parent.IsRed);

                  // BST invariant
                  var expectedCmpNe = n == parentOrNull.Left ? 1 : -1;
                  Assert.NotEquals(expectedCmpNe, Math.Sign(comparer.Compare(n.Value, parent.Value)));
               }
            }

            if (n.Left == null && n.Right == null) {
               leaves.Add(n);
            } else {
               if (n.Left != null) q.Enqueue((n.Left, rootToNodeBlacks + (n.Left.IsBlack ? 1 : 0), n));
               if (n.Right != null) q.Enqueue((n.Right, rootToNodeBlacks + (n.Right.IsBlack ? 1 : 0), n));
            }
         }

         // Every path from a given node to any of its descendant NIL leaves goes through the same number of black nodes.
         var rootToLeafBlackCount = nodeToRootToNodeBlacks[leaves[0]];
         foreach (var leaf in leaves) {
            Assert.Equals(rootToLeafBlackCount, nodeToRootToNodeBlacks[leaf]);
         }

         // Console.WriteLine("Root->Leaf Blacks " + rootToLeafBlackCount);

         // Every node tracks its black height, the number of black nodes to its leaves
         foreach (var (node, rootToNodeBlacks) in nodeToRootToNodeBlacks) {
            // +1 because null children count as black nodes.
            var actualBlackHeight = rootToLeafBlackCount - rootToNodeBlacks + 1;
            Assert.Equals(node.BlackHeight, actualBlackHeight);
         }
      }

      private void DebugVerifyRootInvariants(RedBlackNode<T> root) {
         if (!kEnableDebugVerifyInvariants) return;
         VerifyInvariants(root, true);
      }

      private void DebugVerifyInternalNodeInvariants(RedBlackNode<T> node) {
         if (!kEnableDebugVerifyInvariants) return;
         VerifyInvariants(node, false);
      }
   }
}
