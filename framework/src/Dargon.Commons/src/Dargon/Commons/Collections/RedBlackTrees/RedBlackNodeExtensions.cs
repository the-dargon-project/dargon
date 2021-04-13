using System.Diagnostics;
using Dargon.Commons.Collections.RedBlackTrees;

namespace Dargon.Commons.Collections {
   public static class BlackHeightUtils {
      public static int ComputeForParent<T>(RedBlackNode<T> n) {
         if (n == null) return 1;
         return n.BlackHeight + (n.IsBlack ? 1 : 0);
      }

      public static int Get<T>(RedBlackNode<T> n) {
         if (n == null) return 0;
         return n.BlackHeight;
      }
   }

   public static class RedBlackNodeExtensions {
      public static bool IsNonNullBlack<T>(this RedBlackNode<T> node) => node != null && node.IsBlack;

      public static bool IsNonNullRed<T>(this RedBlackNode<T> node) => node != null && node.IsRed;
      public static bool IsNonNullLeaf<T>(this RedBlackNode<T> node) => node != null && node.IsLeaf;

      public static bool IsNullOrBlack<T>(this RedBlackNode<T> node) => node == null || node.IsBlack;

      public static bool Is4Node<T>(this RedBlackNode<T> node) => IsNonNullRed(node.Left) && IsNonNullRed(node.Right);
      public static bool Is2Node<T>(this RedBlackNode<T> node) => node.IsBlack && IsNullOrBlack(node.Left) && IsNullOrBlack(node.Right);

      /// <summary>
      /// Replaces a child of this node with a new node.
      /// </summary>
      /// <param name="child">The child to replace.</param>
      /// <param name="newChild">The node to replace <paramref name="child"/> with.</param>
      public static void ReplaceChild<T>(this RedBlackNode<T> node, RedBlackNode<T> child, RedBlackNode<T> newChild) {
         if (node.Left == child) {
            node.Left = newChild;
         } else {
            Debug.Assert(node.Right == child);
            node.Right = newChild;
         }
      }

      public static RedBlackNode<T> GetSibling<T>(this RedBlackNode<T> parent, RedBlackNode<T> node) {
         Debug.Assert(node != null);
         Debug.Assert(node == parent.Left ^ node == parent.Right);

         return node == parent.Left ? parent.Right! : parent.Left!;
      }

      public static void Split4Node<T>(this RedBlackNode<T> node) {
         node.Color = RedBlackColor.Red;
         node.BlackHeight++;
         node.Left.Color = RedBlackColor.Black;
         node.Right.Color = RedBlackColor.Black;
      }

      /// <summary>
      /// Combines two 2-nodes into a 4-node.
      /// </summary>
      public static void Merge2Node<T>(this RedBlackNode<T> node) {
         Debug.Assert(node.IsRed);
         Debug.Assert(node.Left!.Is2Node());
         Debug.Assert(node.Right!.Is2Node());

         node.Color = RedBlackColor.Black;
         node.BlackHeight--;
         node.Left.Color = RedBlackColor.Red;
         node.Right.Color = RedBlackColor.Red;
      }

      public static (RedBlackNode<T>, (T, RedBlackColor), RedBlackNode<T>) Expose<T>(this RedBlackNode<T> node) {
         return (node.Left, (node.Value, node.Color), node.Right);
      }
   }
}