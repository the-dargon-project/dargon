using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dargon.Commons.Collections.RedBlackTrees {
   public partial class RedBlackNodeCollectionOperations<T, TComparer> where TComparer : struct, IComparer<T> {
      public RedBlackNode<T> InsertInOrderContiguous(RedBlackNode<T> root, T[] values) {
         return InsertInOrderContiguous(root, values, 0, values.Length);
      }

      public RedBlackNode<T> InsertInOrderContiguous(RedBlackNode<T> root, T[] values, int index, int length) {
         if (length == 0) {
            return root;
         } else if (length == 1) {
            // note for length >= 2, we can't simulate this invoke with tryadd
            // in the case where inserted values are equal according to cmp
            var res = TryAdd(root, values[index]);
            res.Success.AssertIsTrue();
            return res.NewRoot;
         }

         var valuesTree = Treeify(values, index, length);
         if (root == null) {
            return valuesTree;
         }

         var (v0Found, left, temp) = TrySplit(root, values[index]);
         var (vfFound, center, right) = TrySplit(temp, values[index + length - 1]);

         if (center != null) {
            throw new InvalidOperationException("The inserted sequence would not have been in-order contiguous.");
         }

         if (left == null && right == null) {
            // possible if root only had 2 nodes values[index] & values[index + length - 1]
            return valuesTree;
         } else if (left == null) {
            var (rightWithoutFirst, rightFirst) = SplitFirst(right);
            return JoinRB(valuesTree, rightFirst, rightWithoutFirst);
         } else if (right == null) {
            var (leftWithoutLast, leftLast) = SplitLast(left);
            return JoinRB(leftWithoutLast, leftLast, valuesTree);
         } else {
            var (leftWithoutLast, leftLast) = SplitLast(left);
            var (rightWithoutFirst, rightFirst) = SplitFirst(right);
            return JoinRB(JoinRB(leftWithoutLast, leftLast, valuesTree), rightFirst, rightWithoutFirst);
         }
      }
   }
}
