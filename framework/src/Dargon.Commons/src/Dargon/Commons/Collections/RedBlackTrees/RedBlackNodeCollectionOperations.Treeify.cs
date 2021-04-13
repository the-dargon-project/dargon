using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dargon.Commons.Collections.RedBlackTrees {
   public partial class RedBlackNodeCollectionOperations<T, TComparer> where TComparer : struct, IComparer<T> {
      public RedBlackNode<T> Treeify(T[] values)
         => Treeify(values, 0, values.Length);

      public RedBlackNode<T> Treeify(T[] values, int startIndex, int length) {
         startIndex.AssertIsGreaterThanOrEqualTo(0);
         startIndex.AssertIsLessThanOrEqualTo(values.Length);
         length.AssertIsGreaterThanOrEqualTo(0);
         (startIndex + length).AssertIsLessThanOrEqualTo(values.Length);

         if (length == 0) {
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
         var nPlus1 = length + 1;
         var nPlus1IsPowerOf2 = (nPlus1 & (nPlus1 - 1)) == 0;

         var redDepth =
            nPlus1IsPowerOf2
               ? -1
               : (int)Math.Floor(Math.Log2(length) + 1E-5 /* Epsilon maybe unnecessary */);

         return TreeifyInternal(values, startIndex, startIndex + length - 1, 0, redDepth);
      }

      private RedBlackNode<T> TreeifyInternal(T[] values, int lo, int hi, int depth, int redDepth) {
         if (lo > hi) {
            return null;
         }

         var mid = (lo + hi) / 2;
         var left = TreeifyInternal(values, lo, mid - 1, depth + 1, redDepth);
         var right = TreeifyInternal(values, mid + 1, hi, depth + 1, redDepth);

         return new RedBlackNode<T>(
            values[mid],
            depth == redDepth ? RedBlackColor.Red : RedBlackColor.Black,
            left,
            right,
            BlackHeightUtils.ComputeForParent(left));
      }
   }
}
