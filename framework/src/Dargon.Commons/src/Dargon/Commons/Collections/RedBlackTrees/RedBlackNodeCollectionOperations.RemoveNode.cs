using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Dargon.Commons.Exceptions;

namespace Dargon.Commons.Collections.RedBlackTrees {
   public partial class RedBlackNodeCollectionOperations<T> {
      private int GetChildCount(RedBlackNode<T> node) {
         return (node.Left != null ? 1 : 0) + (node.Right != null ? 1 : 0);
      }

      public RedBlackNode<T> RemoveNode(RedBlackNode<T> root, RedBlackNode<T> deletedNode) {
         root.ThrowIfNull("Can't remove a node from a null root");
         deletedNode.ThrowIfNull("Can't delete a null node");

         const bool kEnableDebugPrint = false;

         var deletedNodeChildCount = GetChildCount(deletedNode);

         // ensure node has 0 or 1 children
         if (deletedNodeChildCount == 2) {
            root = SwapWithInOrderPredecessorBreakingInOrderInvariant(root, deletedNode);
         }

         var deletedNodeChild = deletedNode.Left ?? deletedNode.Right;

         // The deleted node gets replaced by its child. (node this code handles singular roots too!)
         var deletedNodeOriginalParent = deletedNode.Parent;
         ReplaceChildOrRoot(deletedNode.Parent, deletedNode, deletedNodeChild, ref root);
         deletedNode.Parent = null;

         // If the deleted node was red, nothing else to do.
         if (deletedNode.IsRed) {
            return root;
         }

         // If we reach here, the deleted node was black. We need to hand its blackness
         // to another node to maintain the RB tree's blackness invariant.
         var parent = deletedNodeOriginalParent;
         var current = deletedNodeChild; // current needs to take in black

         while (true) {
            if (kEnableDebugPrint) Console.WriteLine($"LOOP cur: {current?.Value?.ToString() ?? "[null]"} par: {parent?.Value?.ToString() ?? "[null]"}");
            var cmp = new DummyComparer();
            if (kEnableDebugPrint) DumpToConsoleColored(root, cmp, false);

            if (current.IsNonNullRed()) {
               // case 0: red -> black
               if (kEnableDebugPrint) Console.WriteLine("CASE 0");
               current.Color = RedBlackColor.Black;
               break;
            }

            // Alternatively, is parent is a black root, there's no point marking it double black;
            // making it black will preserve the RB invariants, so done!
            if (parent == null) {
               if (kEnableDebugPrint) Console.WriteLine("CASE DOUBLE BLACK ROOT");
               break;
            }

            var currentIsLeftChild = parent.Left == current;
            var sibling = currentIsLeftChild ? parent.Right : parent.Left;
            var grandparent = parent.Parent;

            if (current.IsNullOrBlack() && sibling.IsNonNullRed()) {
               // case 1:
               if (kEnableDebugPrint) Console.WriteLine("CASE 1");
               sibling.Color = RedBlackColor.Black;
               parent.Color = RedBlackColor.Red;

               var replacement = currentIsLeftChild ? parent.RotateLeft() : parent.RotateRight();
               ReplaceChildOrRoot(grandparent, parent, replacement, ref root);
               replacement.BlackHeight = BlackHeightUtils.ComputeForParent(parent);

               continue;
            } else if (current.IsNullOrBlack() && sibling.IsNonNullBlack() && sibling.Left.IsNullOrBlack() && sibling.Right.IsNullOrBlack()) {
               // case 2:
               if (kEnableDebugPrint) Console.WriteLine("CASE 2");
               sibling.Color = RedBlackColor.Red;

               parent.BlackHeight = BlackHeightUtils.ComputeForParent(sibling);

               current = parent;
               parent = current.Parent;

               // case 2a should be covered in case 0 above
               continue;
            }

            if (currentIsLeftChild) {
               var goCase4 = false;
               if (current.IsNullOrBlack() && sibling.IsNonNullBlack() && sibling.Left.IsNonNullRed() && sibling.Right.IsNullOrBlack()) {
                  // case 3 left
                  if (kEnableDebugPrint) Console.WriteLine("CASE 3");
                  sibling.Left.Color = RedBlackColor.Black;
                  sibling.Color = RedBlackColor.Red;
                  ReplaceChildOrRoot(parent, sibling, sibling.RotateRight(), ref root);
                  
                  goCase4 = true;
                  // sibling.Value.AssertEquals(parent.Right.Value);
                  sibling = parent.Right;
               }

               if (goCase4 || (current.IsNullOrBlack() && sibling.IsNonNullBlack() && sibling.Right.IsNonNullRed())) {
                  if (kEnableDebugPrint) Console.WriteLine("CASE 4");
                  sibling.Color = parent.Color;
                  parent.Color = RedBlackColor.Black;
                  sibling.Right.Color = RedBlackColor.Black;
                  ReplaceChildOrRoot(grandparent, parent, parent.RotateLeft(), ref root);
                  parent.BlackHeight = BlackHeightUtils.ComputeForParent(parent.Left);
                  sibling.BlackHeight = BlackHeightUtils.ComputeForParent(parent);
                  break;
               }
            } else {
               var goCase4 = false;
               if (current.IsNullOrBlack() && sibling.IsNonNullBlack() && sibling.Right.IsNonNullRed() && sibling.Left.IsNullOrBlack()) {
                  // case 3 left
                  if (kEnableDebugPrint) Console.WriteLine("CASE 3");
                  sibling.Right.Color = RedBlackColor.Black;
                  sibling.Color = RedBlackColor.Red;
                  ReplaceChildOrRoot(parent, sibling, sibling.RotateLeft(), ref root);
                  
                  goCase4 = true;
                  // sibling.Value.AssertEquals(parent.Left.Value);
                  sibling = parent.Left;
               }

               if (goCase4 || (current.IsNullOrBlack() && sibling.IsNonNullBlack() && sibling.Left.IsNonNullRed())) {
                  if (kEnableDebugPrint) Console.WriteLine("CASE 4");
                  sibling.Color = parent.Color;
                  parent.Color = RedBlackColor.Black;
                  sibling.Left.Color = RedBlackColor.Black;
                  ReplaceChildOrRoot(grandparent, parent, parent.RotateRight(), ref root);
                  parent.BlackHeight = BlackHeightUtils.ComputeForParent(parent.Right);
                  sibling.BlackHeight = BlackHeightUtils.ComputeForParent(parent);
                  break;
               }
            }
         }

         return root;
      }

      /// <summary>
      /// Pred must be rightmost(succ.Left)
      ///
      /// succ is being deleted. We must preserve red-black properties of the tree
      /// as well as ordering properties for nodes other than n.
      /// </summary>
      private RedBlackNode<T> SwapWithInOrderPredecessorBreakingInOrderInvariant(RedBlackNode<T> root, RedBlackNode<T> succ) {
         var pred = GetRightmost(succ.Left);

         var succWasRoot = succ == root;
         var succParent = succ.Parent;
         var succLeft = succ.Left;
         var succRight = succ.Right;

         var predParent = pred.Parent;
         var predLeft = pred.Left;
         var predRight = pred.Right;

         var succIsLeftChild = succParent != null && succParent.Left == succ;
         var predIsLeftChild = predParent != null && predParent.Left == pred;

         // swap node height / color
         (succ.BlackHeight, pred.BlackHeight) = (pred.BlackHeight, succ.BlackHeight);
         (succ.Color, pred.Color) = (pred.Color, succ.Color);

         // swap node positions
         if (pred == succ.Left) {
            succ.Left = predLeft;
            if (predLeft != null) predLeft.Parent = succ;
            succ.Right = predRight;
            if (predRight != null) predRight.Parent = succ;
            succ.Parent = pred;

            pred.Left = succ;
            pred.Right = succRight;
            if (succRight != null) succRight.Parent = pred;
            pred.Parent = succParent;

            if (succParent != null) {
               if (succIsLeftChild) {
                  succParent.Left = pred;
               } else {
                  succParent.Right = pred;
               }

               return root;
            } else {
               return pred;
            }
         } else {
            succ.Left = predLeft;
            if (predLeft != null) predLeft.Parent = succ;
            succ.Right = predRight;
            if (predRight != null) predRight.Parent = succ;
            succ.Parent = predParent;
            if (predParent != null) {
               if (predIsLeftChild) predParent.Left = succ;
               else predParent.Right = succ;
            }

            pred.Left = succLeft;
            if (succLeft != null) succLeft.Parent = pred;
            pred.Right = succRight;
            if (succRight != null) succRight.Parent = pred;
            pred.Parent = succParent;
            if (succParent != null) {
               if (succIsLeftChild) succParent.Left = pred;
               else succParent.Right = pred;
            }

            if (succWasRoot) {
               return pred;
            } else {
               return root;
            }
         }
      }

      public RedBlackNode<T> GetLeftmost(RedBlackNode<T> node) {
         while (node.Left != null) node = node.Left;
         return node;
      }

      public RedBlackNode<T> GetRightmost(RedBlackNode<T> node) {
         while (node.Right != null) node = node.Right;
         return node;
      }


   }
}

/*
         var nodeChildCount = GetChildCount(node);

         // If N is the root which does not have a non-NIL child, it is replaced by a NIL leaf, after which the tree is empty—and in RB-shape.
         if (node == root && nodeChildCount == 0) {
            return null;
         }

         // If N has two non-NIL children, an additional navigation to either the maximum element in its left subtree (which is the in-order predecessor)
         // or the minimum element in its right subtree (which is the in-order successor) finds a node with no other node in between.
         // Without touching the user data of this node, all red–black tree pointers of this node and N are exchanged so that N now has at most one non-NIL child.
         if (node.Left != null && node.Right != null) {
            var inOrderPredecessor = GetRightmost(node.Left);
            root = RemoveNodeSwapHelper1(root, inOrderPredecessor, node);
            nodeChildCount = GetChildCount(node);
         }

         // If N has exactly one non-NIL child, it must be a red child, because if it were a black one then property 4 would force a second black non-NIL child.
         if (nodeChildCount == 1) {
            var (child, childIsLeft) = node.Left != null ? (node.Left, true) : (node.Right, false);
            
            if (node == root) {
               child.Parent = null;
               node.Left = node.Right = null;
               return child;
            }

            var nodeIsLeft = node.Parent.Left == node;
            if (nodeIsLeft) {
               node.Parent.Left = child;
               child.Parent = node.Parent;
               node.Parent = null;
            } else {
               node.Parent.Right = child;
               child.Parent = node.Parent;
               node.Parent = null;
            }

            return root;
         }

         // If N is a red node, it cannot have a non-NIL child, because this would have to be black by property 3.
         // Moreover, it cannot have exactly one black child as argued just above. As a consequence the red node N is without any child and can simply be removed.
         if (node.IsRed) {
            var nodeIsLeft = node.Parent.Left == node;
            if (nodeIsLeft) node.Parent.Left = null;
            else node.Parent.Right = null;
            node.Parent = null;
            return root;
         }

         // If N is a black node, it may have a red child or no non-NIL child at all. If N has a red child, it is simply replaced with this child after
         // painting the latter black.
         if (node.IsBlack && nodeChildCount == 1) {
            var child = node.Left ?? node.Right;
            if (child.IsRed) {
               var nodeParent = node.Parent;
               if (nodeParent == null) {

               }
               var nodeIsLeft = node.Parent == null;
            }
         }  
*/