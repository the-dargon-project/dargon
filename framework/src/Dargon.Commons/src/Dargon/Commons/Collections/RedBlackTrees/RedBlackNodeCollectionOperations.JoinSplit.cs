using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dargon.Commons.Collections.RedBlackTrees {
   public partial class RedBlackNodeCollectionOperations<T, TComparer> where TComparer : struct, IComparer<T> {
      private RedBlackNode<T> JoinRightRB(RedBlackNode<T> left, RedBlackNode<T> mid, RedBlackNode<T> right) {
         if (BlackHeightUtils.Get(left) == BlackHeightUtils.Get(right)) {
            // return new RedBlackNode<T>(k, RedBlackColor.Red) {
            //    Left = left,
            //    Right = right,
            //    BlackHeight = BlackHeightUtils.ComputeForParent(left),
            // };
            mid.Color = RedBlackColor.Red;
            mid.Left = left;
            mid.Right = right;
            mid.BlackHeight = BlackHeightUtils.ComputeForParent(left);
            return mid;
         }

         var tprime = left;
         tprime.Right = JoinRightRB(left.Right, mid, right);
         if (tprime.Color == RedBlackColor.Black && tprime.Right.IsNonNullRed() && tprime.Right.Right.IsNonNullRed()) {
            tprime.Right.Right.Color = RedBlackColor.Black;

            var res = tprime.RotateLeft();
            res.BlackHeight = BlackHeightUtils.ComputeForParent(res.Left);
            return res;
         }

         return tprime;
      }

      private RedBlackNode<T> JoinLeftRB(RedBlackNode<T> left, RedBlackNode<T> mid, RedBlackNode<T> right) {
         if (BlackHeightUtils.Get(left) == BlackHeightUtils.Get(right)) {
            // return new RedBlackNode<T>(k, RedBlackColor.Red) {
            //    Left = left,
            //    Right = right,
            //    BlackHeight = BlackHeightUtils.ComputeForParent(right),
            // };
            mid.Color = RedBlackColor.Red;
            mid.Left = left;
            mid.Right = right;
            mid.BlackHeight = BlackHeightUtils.ComputeForParent(right);
            return mid;
         }

         var tprime = right;
         tprime.Left = JoinLeftRB(left, mid, right.Left);
         if (tprime.Color == RedBlackColor.Black && tprime.Left.IsNonNullRed() && tprime.Left.Left.IsNonNullRed()) {
            tprime.Left.Left.Color = RedBlackColor.Black;

            var res = tprime.RotateRight();
            res.BlackHeight = BlackHeightUtils.ComputeForParent(res.Right);
            return res;
         }

         return tprime;
      }

      public RedBlackNode<T> JoinRB(RedBlackNode<T> left, RedBlackNode<T> mid, RedBlackNode<T> right) {
         RedBlackNode<T> res;
         if (BlackHeightUtils.Get(left) > BlackHeightUtils.Get(right)) {
            res = JoinRightRB(left, mid, right);
         } else if (BlackHeightUtils.Get(left) < BlackHeightUtils.Get(right)) {
            res = JoinLeftRB(left, mid, right);
         } else if (left.IsNullOrBlack() && right.IsNullOrBlack()) {
            //res = new RedBlackNode<T>(mid, RedBlackColor.Red, left, right, BlackHeightUtils.ComputeForParent(left));
            mid.Color = RedBlackColor.Red;
            mid.Left = left;
            mid.Right = right;
            mid.BlackHeight = BlackHeightUtils.ComputeForParent(left);
            
            res = mid;
         } else {
            // res = new RedBlackNode<T>(mid, RedBlackColor.Black, left, right, BlackHeightUtils.ComputeForParent(left));
            mid.Color = RedBlackColor.Black;
            mid.Left = left;
            mid.Right = right;
            mid.BlackHeight = BlackHeightUtils.ComputeForParent(left);

            res = mid;
         }

         return FixRedJoinRoot(res);
      }

      private RedBlackNode<T> FixRedJoinRoot(RedBlackNode<T> node) {
         if (node == null) {
            return null;
         }

         if (!node.IsRed) {
            return node;
         }

         var leftHeight = BlackHeightUtils.Get(node.Left);
         var rightHeight = BlackHeightUtils.Get(node.Right);

         if (leftHeight == rightHeight) {
            node.Color = RedBlackColor.Black;
            return node;
         } else if (leftHeight < rightHeight) {
            var res = node.RotateLeft();
            res.Color = RedBlackColor.Black;
            res.Left.BlackHeight = BlackHeightUtils.ComputeForParent(res.Left.Right);
            res.BlackHeight = BlackHeightUtils.ComputeForParent(res.Left);
            return res;
         } else {
            var res = node.RotateRight();
            res.Color = RedBlackColor.Black;
            res.Right.BlackHeight = BlackHeightUtils.ComputeForParent(res.Right.Left);
            res.BlackHeight = BlackHeightUtils.ComputeForParent(res.Right);
            return res;
         }
      }

      public (RedBlackNode<T> Left, RedBlackNode<T> Match, RedBlackNode<T> Right) TrySplit(RedBlackNode<T> node, T k) {
         if (node == null) {
            return (null, null, null);
         } else {
            var cmp = comparer.Compare(node.Value, k);
            // Console.WriteLine("cmp " + node.Value + " vs " + k + " " + cmp);
            if (cmp == 0) {
               return (FixRedJoinRoot(node.Left), node, FixRedJoinRoot(node.Right));
            } else if (cmp > 0) {
               var (ll, match, lr) = TrySplit(node.Left, k);
               return (ll, match, JoinRB(lr, node, FixRedJoinRoot(node.Right)));
            } else {
               var (rl, match, rr) = TrySplit(node.Right, k);
               return (JoinRB(FixRedJoinRoot(node.Left), node, rl), match, rr);
            }
         }
      }

      public (RedBlackNode<T> Left, RedBlackNode<T> Last) SplitLast(RedBlackNode<T> node) {
         if (node.Right == null) {
            return (node.Left, node);
         } else {
            var (tprime, kprime) = SplitLast(node.Right);
            // if (kEnableDebugPrintSplitJoin) Console.WriteLine("Split yields " + kprime);
            // if (kEnableDebugPrintSplitJoin) Console.WriteLine("Split joinrb input " + ToGraphvizStringDebug(node.Left));
            // if (kEnableDebugPrintSplitJoin) Console.WriteLine("Split joinrb input " + node.Value);
            // if (kEnableDebugPrintSplitJoin) Console.WriteLine("Split joinrb input " + ToGraphvizStringDebug(tprime));
            var splitTree = JoinRB(node.Left, node, tprime);
            // if (kEnableDebugPrintSplitJoin) Console.WriteLine("Split joinrb out: " + ToGraphvizStringDebug(splitTree));
            return (splitTree, kprime);
         }
      }

      public (RedBlackNode<T> First, RedBlackNode<T> Right) SplitFirst(RedBlackNode<T> node) {
         if (node.Left == null) {
            return (node, node.Right);
         } else {
            var (kprime, tprime) = SplitFirst(node.Left);
            var splitTree = JoinRB(tprime, node, node.Right);
            return (kprime, splitTree);
         }
      }

      public RedBlackNode<T> Join2(RedBlackNode<T> tl, RedBlackNode<T> tr) {
         if (tl == null) return tr;
         if (tr == null) return tl;

         var (tlprime, k) = SplitLast(tl);
         return JoinRB(tlprime, k, tr);
      }
   }
}
