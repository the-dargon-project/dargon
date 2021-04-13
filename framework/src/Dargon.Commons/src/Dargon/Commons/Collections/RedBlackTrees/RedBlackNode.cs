using Dargon.Commons.Collections.RedBlackTrees;

namespace Dargon.Commons.Collections {
   public class RedBlackNode<T> {
      public RedBlackNode<T> Parent;
      public RedBlackNode<T> Left, Right;
      public readonly T Value;
      public RedBlackColor Color;

      // The black height of a black leaf is 0.
      // The black height of the root of a 2-node tree of black nodes is 1.
      public int BlackHeight;

      public RedBlackNode(T value, RedBlackColor color) {
         Value = value;
         Color = color;
         BlackHeight = 1; // a node without children has implicit NIL leaves.
      }

      public RedBlackNode(T value, RedBlackColor color, RedBlackNode<T> left, RedBlackNode<T> right, int blackHeight) {
         Value = value;
         Color = color;
         Left = left;
         Right = right;
         BlackHeight = blackHeight;

         if (Left != null) Left.Parent = this;
         if (Right != null) Right.Parent = this;
      }

      public bool IsRed => Color == RedBlackColor.Red;
      public bool IsBlack => Color == RedBlackColor.Black;
      public bool IsLeaf => Left == null && Right == null;
   }
}