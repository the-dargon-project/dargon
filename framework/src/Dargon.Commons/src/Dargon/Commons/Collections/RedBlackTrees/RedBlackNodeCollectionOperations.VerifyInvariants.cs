using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dargon.Commons.Collections.RedBlackTrees {
   public partial class RedBlackNodeCollectionOperations<T> {
      /// <summary>
      /// Disables runtime checks on invariants that should always pass
      /// </summary>
      protected const bool kEnableDebugVerifyInvariants = false;

      [ThreadStatic] private static Queue<(RedBlackNode<T> n, int rootToNodeBlacks, RedBlackNode<T> parent)> tlsVerifyInvariantsNodeQueue;
      [ThreadStatic] private static List<RedBlackNode<T>> tlsVerifyInvariantsLeavesList;
      [ThreadStatic] private static Dictionary<RedBlackNode<T>, int> tlsVerifyInvariantsNodeToRootToNodeBlacks;

      public void VerifyInvariantsExceptOrdering(RedBlackNode<T> initialNode, bool assertTreeRootInvariants = true) {
         var cmp = new DummyComparer();
         VerifyInvariants(initialNode, cmp, assertTreeRootInvariants, false);
      }

      public void VerifyInvariants<TComparer>(RedBlackNode<T> initialNode, in TComparer comparer, bool assertTreeRootInvariants = true, bool testInOrderInvariants = true)
         where TComparer : struct, IComparer<T> {
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
         var q = tlsVerifyInvariantsNodeQueue ??= new Queue<(RedBlackNode<T> n, int rootToNodeBlacks, RedBlackNode<T> parent)>();
         q.Clear();
         q.Enqueue((initialNode, initialNode.IsBlack ? 1 : 0, null));

         var treeSize = CountNodes(initialNode);
         var leaves = tlsVerifyInvariantsLeavesList ??= new List<RedBlackNode<T>>(treeSize);
         leaves.Clear();

         var nodeToRootToNodeBlacks = tlsVerifyInvariantsNodeToRootToNodeBlacks ??= new Dictionary<RedBlackNode<T>, int>(treeSize);
         nodeToRootToNodeBlacks.Clear();

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
                  if (testInOrderInvariants) {
                     var expectedCmpNe = n == parentOrNull.Left ? 1 : -1;
                     Assert.NotEquals(expectedCmpNe, Math.Sign(comparer.Compare(n.Value, parent.Value)));
                  }
               }
            }

            if (n.Left == null && n.Right == null) {
               leaves.Add(n);
            } else {
               if (n.Left != null) q.Enqueue((n.Left, rootToNodeBlacks + (n.Left.IsBlack ? 1 : 0), n));
               if (n.Right != null) q.Enqueue((n.Right, rootToNodeBlacks + (n.Right.IsBlack ? 1 : 0), n));
            }

            Assert.Equals(n.BlackHeight, BlackHeightUtils.ComputeForParent(n.Left));
            Assert.Equals(n.BlackHeight, BlackHeightUtils.ComputeForParent(n.Right));
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

         tlsVerifyInvariantsNodeQueue.Clear();
         tlsVerifyInvariantsLeavesList.Clear();
         tlsVerifyInvariantsNodeToRootToNodeBlacks.Clear();
      }

      internal void DebugVerifyRootInvariants<TComparer>(RedBlackNode<T> root, in TComparer comparer) where TComparer : struct, IComparer<T> {
         if (!kEnableDebugVerifyInvariants) return;
         VerifyInvariants(root, in comparer, true);
      }

      internal void DebugVerifyInternalNodeInvariants<TComparer>(RedBlackNode<T> node, in TComparer comparer) where TComparer : struct, IComparer<T> {
         if (!kEnableDebugVerifyInvariants) return;
         VerifyInvariants(node, in comparer, false);
      }
   }

   public partial class RedBlackNodeCollectionOperations<T, TComparer> where TComparer : struct, IComparer<T> {
      public void VerifyInvariants(RedBlackNode<T> initialNode, bool assertTreeRootInvariants = true) {
         VerifyInvariants(initialNode, in comparer, assertTreeRootInvariants);
      }

      internal void DebugVerifyRootInvariants(RedBlackNode<T> root) {
         if (!kEnableDebugVerifyInvariants) return;
         VerifyInvariants(root, true);
      }

      internal void DebugVerifyInternalNodeInvariants(RedBlackNode<T> node) {
         if (!kEnableDebugVerifyInvariants) return;
         VerifyInvariants(node, false);
      }
   }
}
