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
      public RedBlackNode<T> GetPredecessorOrNull(RedBlackNode<T> node) {
         if (node.Left != null) {
            return GetRightmost(node.Left);
         }

         // run up the tree while we can
         while (node.Parent != null) {
            if (node.Parent.Right == node) {
               // if we are the right child of our parent,
               // then our parent is our initial node's predecessor.
               return node.Parent;
            } else {
               node = node.Parent;
            }
         }

         // we've run up at the tree and at every step, moved from a left child.
         // the initial node was the leftmost node.
         return null;
      }

      public RedBlackNode<T> GetSuccessorOrNull(RedBlackNode<T> node) {
         if (node.Right != null) {
            return GetLeftmost(node.Right);
         }

         // run up the tree while we can
         while (node.Parent != null) {
            if (node.Parent.Left == node) {
               // if we are the left child of our parent,
               // then our parent is our initial node's successor.
               return node.Parent;
            } else {
               node = node.Parent;
            }
         }

         // we've run up at the tree and at every step, moved from a right child.
         // the initial node was the rightmost node.
         return null;
      }
   }
}
