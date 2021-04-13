using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dargon.Commons.Collections.RedBlackTrees {
   public static class RedBlackNodeCollectionOperationsExtensions {
      public static bool TryAdd<T, TComparer>(this RedBlackNodeCollectionOperations<T, TComparer> ops, ref RedBlackNode<T> root, T value) where TComparer : struct, IComparer<T> {
         var res = ops.TryAdd(root, value);
         root = res.NewRoot;
         return res.Success;
      }
      public static bool TryAdd<T, TComparer>(this RedBlackNodeCollectionOperations<T, TComparer> ops, ref RedBlackNode<T> root, T value, out RedBlackNode<T> newNode) where TComparer : struct, IComparer<T> {
         var res = ops.TryAdd(root, value);
         root = res.NewRoot;
         newNode = res.Node;
         return res.Success;
      }

      public static RedBlackNode<T> AddOrThrow<T, TComparer>(this RedBlackNodeCollectionOperations<T, TComparer> ops, RedBlackNode<T> root, T value) where TComparer : struct, IComparer<T> {
         var res = ops.TryAdd(root, value);
         res.Success.AssertIsTrue();
         return res.NewRoot;
      }

      public static RedBlackNode<T> AddOrThrow<T, TComparer>(this RedBlackNodeCollectionOperations<T, TComparer> ops, RedBlackNode<T> root, T value, out RedBlackNode<T> newNode) where TComparer : struct, IComparer<T> {
         var res = ops.TryAdd(root, value);
         res.Success.AssertIsTrue();
         newNode = res.Node;
         return res.NewRoot;
      }

      public static bool TryRemove<T, TComparer>(this RedBlackNodeCollectionOperations<T, TComparer> ops, ref RedBlackNode<T> root, T value) where TComparer : struct, IComparer<T> {
         var res = ops.TryRemove(root, value);
         root = res.NewRoot;
         return res.Success;
      }

      public static bool TryRemove<T, TComparer>(this RedBlackNodeCollectionOperations<T, TComparer> ops, ref RedBlackNode<T> root, T value, out RedBlackNode<T> removedNode) where TComparer : struct, IComparer<T> {
         var res = ops.TryRemove(root, value);
         root = res.NewRoot;
         removedNode = res.Node;
         return res.Success;
      }

      public static RedBlackNode<T> RemoveOrThrow<T, TComparer>(this RedBlackNodeCollectionOperations<T, TComparer> ops, RedBlackNode<T> root, T value) where TComparer : struct, IComparer<T> {
         var res = ops.TryRemove(root, value);
         res.Success.AssertIsTrue();
         return res.NewRoot;
      }

      public static RedBlackNode<T> RemoveOrThrow<T, TComparer>(this RedBlackNodeCollectionOperations<T, TComparer> ops, RedBlackNode<T> root, T value, out RedBlackNode<T> removedNode) where TComparer : struct, IComparer<T> {
         var res = ops.TryRemove(root, value);
         res.Success.AssertIsTrue();
         removedNode = res.Node;
         return res.NewRoot;
      }
   }
}
