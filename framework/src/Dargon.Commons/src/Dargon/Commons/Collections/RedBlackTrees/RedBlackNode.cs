using Dargon.Commons.Collections.RedBlackTrees;

namespace Dargon.Commons.Collections {
   public class RedBlackNode<T> {
      public RedBlackNode<T> Parent;
      public RedBlackNode<T> Left, Right;
      public T Value;
      public RedBlackColor Color;

      public ref T Data => ref Value;

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

      public RedBlackNode<T> Singleify(RedBlackColor color) {
         Parent = null;
         Left = null;
         Right = null;
         Color = color;
         BlackHeight = BlackHeightUtils.ComputeForParent<T>(null);
         return this;
      }

      public void SetLeftElseRightChild(bool leftElseRight, RedBlackNode<T> replacement) {
         if (replacement != null) replacement.Parent = this;

         if (leftElseRight) {
            Left = replacement;
         } else {
            Right = replacement;
         }
      }

      public static RedBlackNode<T> CreateForInsertion(T val) => new(val, RedBlackColor.Black);
   }

   public static class RedBlackNode {
      public static RedBlackNode<T> CreateForInsertion<T>(T val) => new(val, RedBlackColor.Black);
   }
}