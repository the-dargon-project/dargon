using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Dargon.Commons.Collections.RedBlackTrees {
   public partial class RedBlackNodeCollectionOperations<T, TComparer> where TComparer : struct, IComparer<T> {
      public struct ConstComparerPositive : IComparer<T> {
         [MethodImpl(MethodImplOptions.AggressiveInlining)]
         public int Compare(T x, T y) => 1;
      }

      public struct ConstComparerNegative : IComparer<T> {
         [MethodImpl(MethodImplOptions.AggressiveInlining)]
         public int Compare(T x, T y) => -1;
      }

      /// <summary>
      /// Terminates either with:
      /// 1. Current is nonnull, Match = True
      /// 2. Current is null, Parent is where we'd do an insert.
      ///
      /// Comparer returning -1 descends left, 1 descends right.
      /// </summary>
      private RedBlackNodeSearchResult<T> Search<TCmp2>(RedBlackNode<T> root, T value, bool split4Nodes, in TCmp2 cmp)
         where TCmp2 : struct, IComparer<T> {
         RedBlackNode<T> current = root,
            parent = null,
            grandparent = null,
            greatGrandparent = null;

         int valueVsCurrentComparison = -1;
         int valueVsParentComparison = -1;

         while (current != null) {
            valueVsCurrentComparison = cmp.Compare(value, current.Value);
            if (valueVsCurrentComparison == 0) {
               break;
            }

            if (split4Nodes && current.Is4Node()) {
               current.Split4Node();

               if (parent.IsNonNullRed()) {
                  InsertionBalance(current, ref parent, grandparent, greatGrandparent, ref root);
               }
            }

            greatGrandparent = grandparent;
            grandparent = parent;
            parent = current;
            current = valueVsCurrentComparison < 0 ? current.Left : current.Right;

            valueVsParentComparison = valueVsCurrentComparison;
            valueVsCurrentComparison = -1;
         }

         return new RedBlackNodeSearchResult<T> {
            NewRoot = root,
            Node = current,
            Parent = parent,
            Grandparent = grandparent,
            GreatGrandParent = greatGrandparent,
            Match = valueVsCurrentComparison == 0,
            ValueVsNodeComparison = valueVsCurrentComparison,
            ValueVsParentComparison = valueVsParentComparison,
         };
      }

      // After calling InsertionBalance, we need to make sure `current` and `parent` are up-to-date.
      // It doesn't matter if we keep `grandParent` and `greatGrandParent` up-to-date, because we won't
      // need to split again in the next node.
      // By the time we need to split again, everything will be correctly set.
      private void InsertionBalance(RedBlackNode<T> current, ref RedBlackNode<T> parent, RedBlackNode<T> grandParent, RedBlackNode<T> greatGrandParent, ref RedBlackNode<T> root) {
         Debug.Assert(parent != null);
         Debug.Assert(grandParent != null);

         bool parentIsOnRight = grandParent.Right == parent;
         bool currentIsOnRight = parent.Right == current;

         RedBlackNode<T> newChildOfGreatGrandParent;
         if (parentIsOnRight == currentIsOnRight) {
            // Same orientation, single rotation
            newChildOfGreatGrandParent = currentIsOnRight ? grandParent.RotateLeft() : grandParent.RotateRight();
         } else {
            // Different orientation, double rotation
            newChildOfGreatGrandParent = currentIsOnRight ? grandParent.RotateLeftRight() : grandParent.RotateRightLeft();
            // Current node now becomes the child of `greatGrandParent`
            parent = greatGrandParent;
         }

         // `grandParent` will become a child of either `parent` of `current`.
         grandParent.Color = RedBlackColor.Red;
         newChildOfGreatGrandParent.Color = RedBlackColor.Black;

         ReplaceChildOrRoot(greatGrandParent, grandParent, newChildOfGreatGrandParent, ref root);
      }

      /// <summary>
      /// Replaces the child of a parent node, or replaces the root if the parent is <c>null</c>.
      /// </summary>
      /// <param name="parent">The (possibly <c>null</c>) parent.</param>
      /// <param name="child">The child node to replace.</param>
      /// <param name="newChild">The node to replace <paramref name="child"/> with.</param>
      private void ReplaceChildOrRoot(RedBlackNode<T> parent, RedBlackNode<T> child, RedBlackNode<T> newChild, ref RedBlackNode<T> root) {
         if (parent != null) {
            parent.ReplaceChild(child, newChild);
         } else {
            root = newChild;
            if (root != null) root.Parent = null;
         }
      }

      /// <summary>
      /// Replaces the matching node with its successor.
      /// </summary>
      private void ReplaceNode(RedBlackNode<T> match, RedBlackNode<T> parentOfMatch, RedBlackNode<T> successor, RedBlackNode<T> parentOfSuccessor, ref RedBlackNode<T> root) {
         Debug.Assert(match != null);

         if (successor == match) {
            // This node has no successor. This can only happen if the right child of the match is null.
            Debug.Assert(match.Right == null);
            successor = match.Left!;
         } else {
            Debug.Assert(parentOfSuccessor != null);
            Debug.Assert(successor.Left == null);
            Debug.Assert((successor.Right == null && successor.IsRed) || (successor.Right!.IsRed && successor.IsBlack));

            if (successor.Right != null) {
               successor.Right.Color = RedBlackColor.Black;
            }

            if (parentOfSuccessor != match) {
               // Detach the successor from its parent and set its right child.
               parentOfSuccessor.Left = successor.Right;
               if (parentOfSuccessor.Left != null) parentOfSuccessor.Left.Parent = parentOfSuccessor;
               successor.Right = match.Right;
               if (successor.Right != null) successor.Right.Parent = successor;
            }

            successor.Left = match.Left;
            if (successor.Left != null) successor.Left.Parent = successor;
         }

         if (successor != null) {
            successor.Color = match.Color;
         }

         ReplaceChildOrRoot(parentOfMatch, match, successor!, ref root);

         if (successor != null) {
            successor.BlackHeight = BlackHeightUtils.ComputeForParent(successor.Left);
         }

         if (parentOfMatch != null) {
            parentOfMatch.BlackHeight = BlackHeightUtils.ComputeForParent(successor);
         }
      }

      public (bool Success, RedBlackNode<T> NewRoot, RedBlackNode<T> Node) TryAdd(RedBlackNode<T> root, T value) {
         if (root == null) {
            root = new RedBlackNode<T>(value, RedBlackColor.Black);
            return (true, root, root);
         }

         var search = Search(root, value, true, in comparer);
         root = search.NewRoot;

         if (search.Match) {
            root.Color = RedBlackColor.Black;
            return (false, root, search.Node);
         }

         var node = new RedBlackNode<T>(value, RedBlackColor.Red);
         var parent = search.Parent;
         if (search.ValueVsParentComparison > 0) {
            parent.Right = node;
            node.Parent = parent;
         } else {
            parent.Left = node;
            node.Parent = parent;
         }

         if (parent.IsRed) {
            InsertionBalance(node, ref parent, search.Grandparent, search.GreatGrandParent, ref root);
         }

         root.Color = RedBlackColor.Black;
         return (true, root, node);
      }

      public (RedBlackNode<T> NewRoot, RedBlackNode<T> Node) AddLeft(RedBlackNode<T> root, T value) {
         var node = new RedBlackNode<T>(value, RedBlackColor.Black);
         return (AddLeft(root, node), node);
      }

      public RedBlackNode<T> AddLeft(RedBlackNode<T> root, RedBlackNode<T> valueNode) {
         valueNode.Left.AssertIsNull();
         valueNode.Right.AssertIsNull();
         valueNode.Parent.AssertIsNull();
         valueNode.BlackHeight.AssertEquals(BlackHeightUtils.ComputeForParent<T>(null));

         if (root == null) {
            valueNode.Color = RedBlackColor.Black;
            return valueNode;
         }

         var cmp = new ConstComparerNegative();
         var search = Search(root, valueNode.Value, true, in cmp);
         root = search.NewRoot;
         Assert.IsFalse(search.Match);

         var node = valueNode;
         node.Color = RedBlackColor.Red;

         var parent = search.Parent;
         parent.Left = node;
         node.Parent = parent;

         if (parent.IsRed) {
            InsertionBalance(node, ref parent, search.Grandparent, search.GreatGrandParent, ref root);
         }

         root.Color = RedBlackColor.Black;
         return root;
      }

      public (RedBlackNode<T> NewRoot, RedBlackNode<T> Node) AddRight(RedBlackNode<T> root, T value) {
         var node = new RedBlackNode<T>(value, RedBlackColor.Black);
         return (AddRight(root, node), node);
      }

      public RedBlackNode<T> AddRight(RedBlackNode<T> root, RedBlackNode<T> valueNode) {
         valueNode.Left.AssertIsNull();
         valueNode.Right.AssertIsNull();
         valueNode.Parent.AssertIsNull();
         valueNode.BlackHeight.AssertEquals(BlackHeightUtils.ComputeForParent<T>(null));

         if (root == null) {
            valueNode.Color = RedBlackColor.Black;
            return valueNode;
         }

         var cmp = new ConstComparerPositive();
         var search = Search(root, valueNode.Value, true, in cmp);
         root = search.NewRoot;
         Assert.IsFalse(search.Match);

         var node = valueNode;
         valueNode.Color = RedBlackColor.Red;

         var parent = search.Parent;
         parent.Right = node;
         node.Parent = parent;

         if (parent.IsRed) {
            InsertionBalance(node, ref parent, search.Grandparent, search.GreatGrandParent, ref root);
         }

         root.Color = RedBlackColor.Black;
         return root;
      }


      public (bool Success, RedBlackNode<T> NewRoot, RedBlackNode<T> Node) TryRemove(RedBlackNode<T> root, T item) {
         if (root == null) {
            return (false, root, null);
         }

         // Search for a node and then find its successor.
         // Then copy the item from the successor to the matching node, and delete the successor.
         // If a node doesn't have a successor, we can replace it with its left child (if not empty),
         // or delete the matching node.
         //
         // In top-down implementation, it is important to make sure the node to be deleted is not a 2-node.
         // Following code will make sure the node on the path is not a 2-node.

         RedBlackNode<T> current = root;
         RedBlackNode<T> parent = null;
         RedBlackNode<T> grandParent = null;
         RedBlackNode<T> match = null;
         RedBlackNode<T> parentOfMatch = null;
         bool foundMatch = false;
         while (current != null) {
            if (current.Is2Node<T>()) {
               // Fix up 2-node
               if (parent == null) {
                  // `current` is the root. Mark it red.
                  current.Color = RedBlackColor.Red;
               } else {
                  RedBlackNode<T> sibling = parent.GetSibling(current);
                  if (sibling.IsRed) {
                     // If parent is a 3-node, flip the orientation of the red link.
                     // We can achieve this by a single rotation.
                     // This case is converted to one of the other cases below.
                     Debug.Assert(parent.IsBlack);
                     if (parent.Right == sibling) {
                        parent.RotateLeft();
                     } else {
                        parent.RotateRight();
                     }

                     // parent from black to red, sibling from red to black.
                     parent.Color = RedBlackColor.Red;
                     // parent.BlackHeight++;
                     sibling.Color = RedBlackColor.Black; // The red parent can't have black children.
                     // `sibling` becomes the child of `grandParent` or `root` after rotation. Update the link from that node.
                     ReplaceChildOrRoot(grandParent, parent, sibling, ref root);
                     // `sibling` will become the grandparent of `current`.
                     grandParent = sibling;
                     if (parent == match) {
                        parentOfMatch = sibling;
                     }

                     sibling = parent.GetSibling(current);
                  }

                  Debug.Assert(sibling.IsNonNullBlack());

                  if (sibling.Is2Node<T>()) {
                     parent.Merge2Node();
                  } else {
                     // `current` is a 2-node and `sibling` is either a 3-node or a 4-node.
                     // We can change the color of `current` to red by some rotation.
                     RedBlackNode<T> newGrandParent = parent.Rotate(parent.GetRotation(current, sibling))!;

                     newGrandParent.Color = parent.Color;
                     parent.Color = RedBlackColor.Black;
                     current.Color = RedBlackColor.Red;

                     parent.BlackHeight = BlackHeightUtils.ComputeForParent(current);
                     newGrandParent.BlackHeight = BlackHeightUtils.ComputeForParent(parent);

                     ReplaceChildOrRoot(grandParent, parent, newGrandParent, ref root);
                     if (parent == match) {
                        parentOfMatch = newGrandParent;
                     }

                     grandParent = newGrandParent;
                  }
               }
            }

            // We don't need to compare after we find the match.
            int order = foundMatch ? -1 : comparer.Compare(item, current.Value);
            if (order == 0) {
               // Save the matching node.
               foundMatch = true;
               match = current;
               parentOfMatch = parent;
            }

            grandParent = parent;
            parent = current;
            // If we found a match, continue the search in the right sub-tree.
            current = order < 0 ? current.Left : current.Right;
         }

         // Move successor to the matching node position and replace links.
         if (match != null) {
            ReplaceNode(match, parentOfMatch!, parent!, grandParent!, ref root);
         }

         if (root != null) {
            root.Color = RedBlackColor.Black;
         }

         return (foundMatch, root, match);
      }
   }

   public struct RedBlackNodeSearchResult<T> {
      public RedBlackNode<T> NewRoot;
      public RedBlackNode<T> Node, Parent, Grandparent, GreatGrandParent;
      public bool Match;
      public int ValueVsNodeComparison;
      public int ValueVsParentComparison;
   }
}
