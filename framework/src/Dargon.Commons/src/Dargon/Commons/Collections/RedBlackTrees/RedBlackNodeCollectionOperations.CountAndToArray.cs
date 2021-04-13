﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dargon.Commons.Collections.RedBlackTrees {
   public partial class RedBlackNodeCollectionOperations<T, TComparer> where TComparer : struct, IComparer<T> {
      public int CountNodes(RedBlackNode<T> root) {
         return root == null ? 0 : 1 + CountNodes(root.Left) + CountNodes(root.Right);
      }

      public T[] ToArray(RedBlackNode<T> root) {
         return ToArray(root, CountNodes(root));
      }

      public T[] ToArray(RedBlackNode<T> root, int count) {
         var res = new T[count];
         var nextIndex = 0;
         ToArrayHelper(root, res, ref nextIndex);
         Assert.Equals(nextIndex, res.Length);
         return res;
      }

      private void ToArrayHelper(RedBlackNode<T> current, T[] res, ref int nextIndex) {
         if (current == null) return;
         ToArrayHelper(current.Left, res, ref nextIndex);
         res[nextIndex++] = current.Value;
         ToArrayHelper(current.Right, res, ref nextIndex);
      }
   }
}
