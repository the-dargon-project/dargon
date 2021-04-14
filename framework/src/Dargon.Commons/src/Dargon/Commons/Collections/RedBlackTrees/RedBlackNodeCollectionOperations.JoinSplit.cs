using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dargon.Commons.Collections.RedBlackTrees {
   public partial class RedBlackNodeCollectionOperations<T, TComparer> where TComparer : struct, IComparer<T> {
      /// <summary>
      /// Join R into L, inserting value M, where Values(L) &lt; value(M) &lt; Values(R).
      /// Do this by walking down the right spine of L.
      /// </summary>
      private RedBlackNode<T> JoinRightIntoLeftRB(RedBlackNode<T> left, RedBlackNode<T> mid, RedBlackNode<T> right) {
         Assert.IsTrue(right.IsNullOrBlack());

         var insertee = right;
         var inserteeHeight = BlackHeightUtils.Get(insertee);
         var root = left;
         var current = root;

         RedBlackNode<T> parent = null, grandparent = null, greatGrandparent = null;
         while (BlackHeightUtils.Get(current) != inserteeHeight) {
            if (current.Is4Node()) {
               current.Split4Node();

               if (parent.IsNonNullRed()) {
                  InsertionBalance(current, ref parent, grandparent, greatGrandparent, ref root);
               }
            }

            greatGrandparent = grandparent;
            grandparent = parent;
            parent = current;
            current = current.Right;
         }

         // blackHeight(current) == blackHeight(insertee)
         // parent is either a 2-node (red) or a 3-node (red with 1 black / 1 red child).
         Assert.IsFalse(parent.Is4Node());
         Assert.IsNotNull(parent);

         Assert.Equals(parent.Right, current);

         if (parent.IsRed) {
            // parent is 2 node (red)
            // note its spine-side child (current) and the insertee are both black, so insert a red and rebalance.
            mid.Color = RedBlackColor.Red;
            mid.Parent = parent;
            mid.Left = current;
            mid.Right = insertee;
            mid.BlackHeight = parent.BlackHeight;

            parent.Right = mid;
            if (mid.Left != null) mid.Left.Parent = mid;
            if (mid.Right != null) mid.Right.Parent = mid;

            InsertionBalance(mid, ref parent, grandparent, greatGrandparent, ref root);
         } else {
            // parent is 3 node (black with 1 red 1 black children)
            // both children have black-height matching insertee
            // if its spine-side child is black, insertion is trivial (use a red mid)
            if (current.IsBlack) {
               // trivial insertion
               mid.Color = RedBlackColor.Red;
               mid.Parent = parent;
               mid.Left = current;
               mid.Right = right;
               mid.BlackHeight = parent.BlackHeight;

               parent.Right = mid;
               if (mid.Left != null) mid.Left.Parent = mid;
               if (mid.Right != null) mid.Right.Parent = mid;
            } else {
               // else, its spine-side child is red (with the same BH as insertee) and the
               // sibling is black. 
               //
               // (note c is current below)
               // e.g.  a(B)
               //     b(B) c(R) (right spine)
               //    d e  f(B) g(B)
               //
               // Simple solution:
               //          c(B)
               //     a(R)       M(R)
               //    b(B) f(B) g(B) insertee
               //   d e
               var a = parent.AssertIsNotNull();
               // var b = parent.Left; // unchanged
               var c = current.AssertIsNotNull();
               var f = current.Left; // can be null
               var g = current.Right; // can be null

               if (grandparent == null) {
                  c.Parent = null;
                  root = c;
               } else {
                  grandparent.Right = c;
                  c.Parent = grandparent;
               }

               c.Color = RedBlackColor.Black;
               c.Left = a;
               a.Parent = c;

               a.Color = RedBlackColor.Red;
               a.Right = f;
               if (f != null) f.Parent = a;
               
               c.Right = mid;
               mid.Parent = c;

               mid.Left = g;
               if (g != null) g.Parent = mid;

               mid.Right = insertee;
               insertee.Parent = mid;

               mid.Color = RedBlackColor.Red;
               mid.BlackHeight = c.BlackHeight;
            }
         }

         return root;
      }

      /// <summary>
      /// <seealso cref="JoinRightIntoLeftRB"/> for source of truth
      /// </summary>
      private RedBlackNode<T> JoinLeftIntoRightRB(RedBlackNode<T> left, RedBlackNode<T> mid, RedBlackNode<T> right) {
         Assert.IsTrue(left.IsNullOrBlack());

         var insertee = left;
         var inserteeHeight = BlackHeightUtils.Get(insertee);
         var root = right;
         var current = root;

         RedBlackNode<T> parent = null, grandparent = null, greatGrandparent = null;
         while (BlackHeightUtils.Get(current) != inserteeHeight) {
            if (current.Is4Node()) {
               current.Split4Node();

               if (parent.IsNonNullRed()) {
                  InsertionBalance(current, ref parent, grandparent, greatGrandparent, ref root);
               }
            }

            greatGrandparent = grandparent;
            grandparent = parent;
            parent = current;
            current = current.Left;
         }

         // blackHeight(current) == blackHeight(insertee)
         // parent is either a 2-node (red) or a 3-node (red with 1 black / 1 red child).
         Assert.IsFalse(parent.Is4Node());
         Assert.IsNotNull(parent);

         Assert.Equals(parent.Left, current);

         if (parent.IsRed) {
            mid.Color = RedBlackColor.Red;
            mid.Parent = parent;
            mid.Right = current;
            mid.Left = insertee;
            mid.BlackHeight = parent.BlackHeight;

            parent.Left = mid;
            if (mid.Right != null) mid.Right.Parent = mid;
            if (mid.Left != null) mid.Left.Parent = mid;

            InsertionBalance(mid, ref parent, grandparent, greatGrandparent, ref root);
         } else {
            if (current.IsBlack) {
               mid.Color = RedBlackColor.Red;
               mid.Parent = parent;
               mid.Right = current;
               mid.Left = left;
               mid.BlackHeight = parent.BlackHeight;

               parent.Left = mid;
               if (mid.Right != null) mid.Right.Parent = mid;
               if (mid.Left != null) mid.Left.Parent = mid;
            } else {
               var a = parent.AssertIsNotNull();
               // var b = parent.Right; // unchanged
               var c = current.AssertIsNotNull();
               var f = current.Right; // can be null
               var g = current.Left; // can be null

               if (grandparent == null) {
                  c.Parent = null;
                  root = c;
               } else {
                  grandparent.Left = c;
                  c.Parent = grandparent;
               }

               c.Color = RedBlackColor.Black;
               c.Right = a;
               a.Parent = c;

               a.Color = RedBlackColor.Red;
               a.Left = f;
               if (f != null) f.Parent = a;

               c.Left = mid;
               mid.Parent = c;

               mid.Right = g;
               if (g != null) g.Parent = mid;

               mid.Left = insertee;
               insertee.Parent = mid;

               mid.Color = RedBlackColor.Red;
               mid.BlackHeight = c.BlackHeight;
            }
         }

         return root;
      }

      public RedBlackNode<T> JoinRB(RedBlackNode<T> left, RedBlackNode<T> mid, RedBlackNode<T> right) {
         if (left == null && right == null) {
            mid.Color = RedBlackColor.Black;
            mid.Parent = mid.Left = mid.Right = null;
            mid.BlackHeight = BlackHeightUtils.ComputeForParent<T>(null);
            return mid;
         } else if (left == null) {
            return AddLeft(right, mid);
         } else if (right == null) {
            return AddRight(left, mid);
         }

         RedBlackNode<T> res;
         if (BlackHeightUtils.Get(left) > BlackHeightUtils.Get(right)) {
            res = JoinRightIntoLeftRB(left, mid, right);
         } else if (BlackHeightUtils.Get(left) < BlackHeightUtils.Get(right)) {
            res = JoinLeftIntoRightRB(left, mid, right);
         } else if (left.IsNullOrBlack() && right.IsNullOrBlack()) {
            //res = new RedBlackNode<T>(mid, RedBlackColor.Red, left, right, BlackHeightUtils.ComputeForParent(left));
            mid.Color = RedBlackColor.Red;
            mid.Left = left;
            if (left != null) left.Parent = mid;
            mid.Right = right;
            if (right != null) right.Parent = mid;
            mid.BlackHeight = BlackHeightUtils.ComputeForParent(left);
            
            res = mid;
         } else {
            // res = new RedBlackNode<T>(mid, RedBlackColor.Black, left, right, BlackHeightUtils.ComputeForParent(left));
            mid.Color = RedBlackColor.Black;
            mid.Left = left;
            if (left != null) left.Parent = mid;
            mid.Right = right;
            if (right != null) right.Parent = mid;
            mid.BlackHeight = BlackHeightUtils.ComputeForParent(left);

            res = mid;
         }

         return FixRedJoinRoot(res);
      }

      private RedBlackNode<T> FixRedJoinRoot(RedBlackNode<T> node) {
         if (node == null) {
            return null;
         }

         node.Parent = null;

         if (!node.IsRed) {
            return node;
         }

         var leftHeight = BlackHeightUtils.Get(node.Left);
         var rightHeight = BlackHeightUtils.Get(node.Right);

         node.Color = RedBlackColor.Black;

         if (leftHeight == rightHeight) {
            Assert.Equals(BlackHeightUtils.ComputeForParent(node.Left), BlackHeightUtils.Get(node));
            return node;
         } else if (leftHeight < rightHeight) {
            var res = node.RotateLeft();
            res.Color = RedBlackColor.Black;
            res.Left.BlackHeight = BlackHeightUtils.ComputeForParent(res.Left.Right);
            res.BlackHeight = BlackHeightUtils.ComputeForParent(res.Left);
            res.Parent = null;
            return res;
         } else {
            var res = node.RotateRight();
            res.Color = RedBlackColor.Black;
            res.Right.BlackHeight = BlackHeightUtils.ComputeForParent(res.Right.Left);
            res.BlackHeight = BlackHeightUtils.ComputeForParent(res.Right);
            res.Parent = null;
            return res;
         }
      }

      public (RedBlackNode<T> Left, RedBlackNode<T> Match, RedBlackNode<T> Right) TrySplit(RedBlackNode<T> node, T k) {
         if (node == null) {
            return (null, null, null);
         } else {
            var cmp = comparer.Compare(node.Value, k);
            // Console.WriteLine("cmp " + node.Value + " vs " + k + " " + cmp);
            var left = node.Left;
            var right = node.Right;
            if (cmp == 0) {
               return (Rootify(left), Singleify(node), Rootify(right));
            } else if (cmp > 0) {
               var (ll, match, lr) = TrySplit(left, k);
               return (ll, match, JoinRB(lr, Singleify(node), Rootify(right)));
            } else {
               var (rl, match, rr) = TrySplit(right, k);
               return (JoinRB(Rootify(left), Singleify(node), rl), match, rr);
            }
         }
      }

      public (RedBlackNode<T> Left, RedBlackNode<T> Last) SplitLast(RedBlackNode<T> node) {
         DebugVerifyRootInvariants(node);
         return SplitLastInternal(node);
      }

      private (RedBlackNode<T> Left, RedBlackNode<T> Last) SplitLastInternal(RedBlackNode<T> node) {
         DebugVerifyInternalNodeInvariants(node);

         if (node.Right == null) {
            var left = node.Left;
            return (Rootify(left), Singleify(node));
         } else {
            var (remainder, last) = SplitLastInternal(node.Right);
            var left = node.Left;
            var splitTree = JoinRB(Rootify(left), Singleify(node), remainder);
            return (splitTree, last);
         }
      }

      /// <summary>
      /// Ensures node is a valid tree root
      /// </summary>
      private RedBlackNode<T> Rootify(RedBlackNode<T> node) {
         if (node == null) {
            return node;
         }

         node.Color = RedBlackColor.Black;
         node.Parent = null;
         return node;
      }

      /// <summary>
      /// Ensures node is a valid single-node tree
      /// </summary>
      private RedBlackNode<T> Singleify(RedBlackNode<T> node) {
         node.AssertIsNotNull();

         node.Color = RedBlackColor.Black;
         node.Parent = node.Left = node.Right = null;
         node.BlackHeight = BlackHeightUtils.ComputeForParent<T>(null);
         return node;
      }

      public (RedBlackNode<T> First, RedBlackNode<T> Right) SplitFirst(RedBlackNode<T> node) {
         DebugVerifyRootInvariants(node);
         return SplitFirstInternal(node);
      }

      private (RedBlackNode<T> First, RedBlackNode<T> Right) SplitFirstInternal(RedBlackNode<T> node) {
         DebugVerifyInternalNodeInvariants(node);

         if (node.Left == null) {
            var right = node.Right;
            return (Singleify(node), Rootify(right));
         } else {
            var (first, remainder) = SplitFirstInternal(node.Left);
            var right = node.Right;
            var splitTree = JoinRB(remainder, Singleify(node), Rootify(right));
            return (first, splitTree);
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
