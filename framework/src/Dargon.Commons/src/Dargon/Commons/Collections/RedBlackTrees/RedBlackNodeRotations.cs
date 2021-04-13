using System;
using System.Diagnostics;
using Dargon.Commons.Collections.RedBlackTrees;

namespace Dargon.Commons.Collections {
   public static class RedBlackNodeRotations {
      private const bool kEnableDebugPrint = false;

      /// <summary>
      /// Gets the rotation this node should undergo during a removal.
      /// </summary>
      public static TreeRotation GetRotation<T>(this RedBlackNode<T> node, RedBlackNode<T> current, RedBlackNode<T> sibling) {
         Debug.Assert(sibling.Left.IsNonNullRed() || sibling.Right.IsNonNullRed());
#if DEBUG
                Debug.Assert(HasChildren(current, sibling));
#endif

         bool currentIsLeftChild = node.Left == current;
         return sibling.Left.IsNonNullRed()
            ? (currentIsLeftChild ? TreeRotation.RightLeft : TreeRotation.Right)
            : (currentIsLeftChild ? TreeRotation.Left : TreeRotation.LeftRight);
      }

      /// <summary>
      /// Does a rotation on this tree. May change the color of a grandchild from red to black.
      /// </summary>
      public static RedBlackNode<T> Rotate<T>(this RedBlackNode<T> node, TreeRotation rotation) {
         RedBlackNode<T> removeRed;
         switch (rotation) {
            case TreeRotation.Right:
               removeRed = node.Left!.Left!;
               Debug.Assert(removeRed.IsRed);
               removeRed.Color = RedBlackColor.Black;
               return node.RotateRight();
            case TreeRotation.Left:
               removeRed = node.Right!.Right!;
               Debug.Assert(removeRed.IsRed);
               removeRed.Color = RedBlackColor.Black;
               return node.RotateLeft();
            case TreeRotation.RightLeft:
               Debug.Assert(node.Right!.Left!.IsRed);
               return node.RotateRightLeft();
            case TreeRotation.LeftRight:
               Debug.Assert(node.Left!.Right!.IsRed);
               return node.RotateLeftRight();
            default:
               throw new InvalidOperationException($"{nameof(rotation)}: {rotation} is not a defined {nameof(TreeRotation)} value.");
         }
      }

      /// <summary>
      /// Does a left rotation on this tree, making this node the new left child of the current right child.
      /// </summary>
      public static RedBlackNode<T> RotateLeft<T>(this RedBlackNode<T> node) {
         if (kEnableDebugPrint) Console.WriteLine("rotL");

         RedBlackNode<T> child = node.Right!;
         node.Right = child.Left;
         child.Left = node;

         return child;
      }

      /// <summary>
      /// Does a left-right rotation on this tree. The left child is rotated left, then this node is rotated right.
      /// </summary>
      public static RedBlackNode<T> RotateLeftRight<T>(this RedBlackNode<T> node) {
         if (kEnableDebugPrint) Console.WriteLine("rotLR");
         RedBlackNode<T> child = node.Left!;
         RedBlackNode<T> grandChild = child.Right!;

         node.Left = grandChild.Right;
         grandChild.Right = node;
         child.Right = grandChild.Left;
         grandChild.Left = child;

         return grandChild;
      }

      /// <summary>
      /// Does a right rotation on this tree, making this node the new right child of the current left child.
      /// </summary>
      public static RedBlackNode<T> RotateRight<T>(this RedBlackNode<T> node) {
         if (kEnableDebugPrint) Console.WriteLine("rotR");
         RedBlackNode<T> child = node.Left!;
         node.Left = child.Right;
         child.Right = node;

         return child;
      }

      /// <summary>
      /// Does a right-left rotation on this tree. The right child is rotated right, then this node is rotated left.
      /// </summary>
      public static RedBlackNode<T> RotateRightLeft<T>(this RedBlackNode<T> node) {
         if (kEnableDebugPrint) Console.WriteLine("rotRL");
         RedBlackNode<T> child = node.Right!;
         RedBlackNode<T> grandChild = child.Left!;

         node.Right = grandChild.Left;
         grandChild.Left = node;
         child.Left = grandChild.Right;
         grandChild.Right = child;

         return grandChild;
      }

   }
}