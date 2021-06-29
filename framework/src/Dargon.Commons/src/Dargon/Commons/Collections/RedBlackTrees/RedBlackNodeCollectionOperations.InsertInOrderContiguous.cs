using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dargon.Commons.Collections.RedBlackTrees {
   public partial class RedBlackNodeCollectionOperations<T> {
      public (bool Success, RedBlackNode<T> NewRoot) AddContiguous<TComparer>(RedBlackNode<T> root, T[] values, in TComparer comparer)
         where TComparer : struct, IComparer<T> {
         const bool kEnableDebugPrint = false;

         if (root == null) {
            root = Treeify(values);
            return (true, root);
         } else if (values.Length == 0) {
            return (true, root);
         } else if (values.Length == 1) {
            var res = TryAdd(root, values[0], in comparer);
            return (res.Success, res.NewRoot);
         }

         var inserteeTreeifyBlackHeight = ComputeTreeifyRootBlackHeight(values.Length - 1);

         // If insertee is bigger than current root, it'll probably be faster to treeify insertee,
         // split root, and perform two joins than it will be to insert all of the insertee's values
         // into the new tree.
         //
         // The complexities are:
         //   InsertInOrderContiguousViaJoinSplit: O(logN) split + O(k) treeify + 2 O(logN/k) joins
         //   ThisMethod: O(logN) search + k amortized O(1) insert-and-rebalance operations.
         // 
         // For a sufficiently large enough insertee count, rebalancing's constant factor is going
         // to be bigger than treeify & joins.
         if (inserteeTreeifyBlackHeight >= root.BlackHeight) {
            return (true, InsertInOrderContiguousViaJoinSplit(root, values, in comparer));
         }

         if (kEnableDebugPrint) Console.WriteLine("================================");
         if (kEnableDebugPrint) DumpToConsoleColoredWithoutTestingInOrderInvariants(root);
         if (kEnableDebugPrint) Console.WriteLine("INSERTING " + values.Join(","));
         if (kEnableDebugPrint) DumpToConsoleColoredWithoutTestingInOrderInvariants(root);

         if (kEnableDebugPrint) Console.WriteLine("INSERT " + values[0]);
         root = this.AddOrThrow(root, values[0], out var lastNode, in comparer);
         if (kEnableDebugPrint) DumpToConsoleColoredWithoutTestingInOrderInvariants(root);

         for (var i = 1; i < values.Length; i++) {
            // assertion proves search is constant time. Since rebalancing is amortized O(1),
            // then for a large insertee size k, addcontiguous is 1 logN (search) + k O(1) AddSuccessors.
            lastNode.BlackHeight.AssertIsLessThanOrEqualTo(2);

            if (kEnableDebugPrint) Console.WriteLine("INSERT " + values[i]);
            root = AddSuccessor(root, lastNode, values[i], out var newNode);
            if (kEnableDebugPrint) DumpToConsoleColoredWithoutTestingInOrderInvariants(root);
            lastNode = newNode;
         }

         return (true, root);

         #region dead code

         // // Find a node of the same blackheight as insertee, and merge-insert.
         // // Search either returns:
         // // 1. A matching node, which is an unsupported case (where we insert is undefined)
         // //        => throw
         // // 2. A node of the desired black height
         // //    case 2a: with parent = null because the node is root (side-by-side insertion not implemented)
         // //        => fallback to split/join (the insertee tree is too big)
         // //    case 2b: with parent != null where we can do a side-by-side insertion
         // //        => optimize via tree insertion
         // // 3. No matching node (current = null), with a parent where insertion should happen
         // //    In this case, we never hit case 3, so the tree isn't tall enough to do a fast insertion
         // //    so we fallback to split/join. Identical to case 2a.
         // var searchResult = Search(root, values[0], true, in comparer, inserteeTreeifyBlackHeight);
         // searchResult.ValueVsNodeComparison.AssertNotEquals(0); // case 1
         //
         // // Hm, my logic doesn't work - you can't just insert at the level matching treeify, because
         // // the inserted tree might be between two nodes of a descendent (so you can't just claim the
         // // inserted tree is to the left or the right of a child)...
         // // I suspect it's possible to find the insertion point for a singular value, insert the entire
         // // insertee tree there, then pull it up the tree, in a way more efficient than split/join.
         // //
         // // Alternatively, while search is O(logN), insertion without rebalancing is O(1) and rebalancing
         // // is O(logN) worst case but amortized O(1). For this reason it might be possible to do 
         // // a single logN search, then k amortized O(1) insertions with rebalances after a node? The difficulty
         // // there is doing rotations such that we always have a leaf to correctly insert at...?
         // //
         // // Additionally, you can probably search til blackHeight - 1, then do a split/join down there.
         // // The benefit is you split/join less of the larger tree, so it should be significantly faster.
         // var alwaysHack = true;
         // if (alwaysHack || searchResult.Node == null || searchResult.Parent == null) {
         //    root = InsertInOrderContiguousViaJoinSplit(
         //       searchResult.NewRoot,
         //       values);
         //    return (true, root);
         // }
         //
         // searchResult.Node.BlackHeight.AssertEquals(inserteeTreeifyBlackHeight);
         //
         // if (searchResult.ValueVsNodeComparison < 0) {
         //    // our "join" node is replacing the left child, which will go to its right.
         //    // (alternatively, our values are lesser than the left child)
         //    root = MergeLeftTree(
         //       searchResult.Parent,
         //       searchResult.Grandparent,
         //       searchResult.GreatGrandParent,
         //       searchResult.NewRoot,
         //       new RedBlackNode<T>(values[^1], RedBlackColor.Red),
         //       Treeify(values, 0, values.Length - 1));
         //    return (true, root);
         // } else {
         //    // our "join" node is replacing the right child, which will become the join's left child.
         //    root = MergeRightTree(
         //       searchResult.Parent,
         //       searchResult.Grandparent,
         //       searchResult.GreatGrandParent,
         //       searchResult.NewRoot,
         //       new RedBlackNode<T>(values[0], RedBlackColor.Red),
         //       Treeify(values, 1, values.Length - 1));
         //    return (true, root);
         // }

         #endregion
      }

      public RedBlackNode<T> InsertInOrderContiguousViaJoinSplit<TComparer>(RedBlackNode<T> root, T[] values, in TComparer comparer)
         where TComparer : struct, IComparer<T> {
         return InsertInOrderContiguousViaJoinSplit(root, values, 0, values.Length, in comparer);
      }

      public RedBlackNode<T> InsertInOrderContiguousViaJoinSplit<TComparer>(RedBlackNode<T> root, T[] values, int index, int length, in TComparer comparer)
         where TComparer : struct, IComparer<T> {
         if (length == 0) {
            return root;
         } else if (length == 1) {
            // note for length >= 2, we can't simulate this invoke with tryadd
            // in the case where inserted values are equal according to cmp
            var res = TryAdd(root, values[index], in comparer);
            res.Success.AssertIsTrue();
            return res.NewRoot;
         }

         var valuesTree = Treeify(new Span<T>(values, index, length));
         if (root == null) {
            return valuesTree;
         }

         var (left, v0Match, temp) = TrySplit(root, values[index], in comparer);
         var (center, vfMatch, right) = TrySplit(temp, values[index + length - 1], in comparer);

         v0Match.AssertIsNull();
         vfMatch.AssertIsNull();

         if (center != null) {
            throw new InvalidOperationException("The inserted sequence would not have been in-order contiguous.");
         }

         if (left == null && right == null) {
            // possible if root only had 2 nodes values[index] & values[index + length - 1]
            return valuesTree;
         } else if (left == null) {
            var (rightFirst, rightWithoutFirst) = SplitFirst(right);
            return JoinRB(valuesTree, rightFirst, rightWithoutFirst);
         } else if (right == null) {
            var (leftWithoutLast, leftLast) = SplitLast(left);
            return JoinRB(leftWithoutLast, leftLast, valuesTree);
         } else {
            var (leftWithoutLast, leftLast) = SplitLast(left);
            var (rightFirst, rightWithoutFirst) = SplitFirst(right);
            return JoinRB(JoinRB(leftWithoutLast, leftLast, valuesTree), rightFirst, rightWithoutFirst);
         }
      }
   }

   public partial class RedBlackNodeCollectionOperations<T, TComparer> where TComparer : struct, IComparer<T> {
      public (bool Success, RedBlackNode<T> NewRoot) AddContiguous(RedBlackNode<T> root, T[] values) 
         => AddContiguous(root, values, in comparer);

      public RedBlackNode<T> InsertInOrderContiguousViaJoinSplit(RedBlackNode<T> root, T[] values)
         => InsertInOrderContiguousViaJoinSplit(root, values, in comparer);

      public RedBlackNode<T> InsertInOrderContiguousViaJoinSplit(RedBlackNode<T> root, T[] values, int index, int length)
         => InsertInOrderContiguousViaJoinSplit(root, values, index, length, in comparer);
   }
}
