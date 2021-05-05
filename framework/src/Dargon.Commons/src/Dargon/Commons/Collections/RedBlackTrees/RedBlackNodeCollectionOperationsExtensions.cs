using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dargon.Commons.Collections.RedBlackTrees {
   public static class RedBlackNodeCollectionOperationsExtensions {
      public static RedBlackNodeSearchResult<T> Search<T, TCmp2>(this RedBlackNodeCollectionOperations<T> ops, ref RedBlackNode<T> root, T value, bool split4Nodes, in TCmp2 cmp) where TCmp2 : struct, IComparer<T> {
         var res = ops.Search(root, new RedBlackNodeCollectionOperations<T>.StructComparerWrappingSearchComparer<TCmp2>(cmp, value), split4Nodes);
         root = res.NewRoot;
         return res;
      }

      public static RedBlackNodeSearchResult<T> Search<T, TSearchComparer>(this RedBlackNodeCollectionOperations<T> ops, ref RedBlackNode<T> root, in TSearchComparer cmp, bool split4Nodes) where TSearchComparer : struct, IBstSearchComparer<T> {
         var res = ops.Search(root, in cmp, split4Nodes);
         root = res.NewRoot;
         return res;
      }
      public static RedBlackNodeSearchResult<T> Search<T, TComparer>(this RedBlackNodeCollectionOperations<T, TComparer> ops, ref RedBlackNode<T> root, T value, bool split4Nodes) where TComparer : struct, IComparer<T> {
         var res = ops.Search(root, value, split4Nodes);
         root = res.NewRoot;
         return res;
      }

      public static bool TryAdd<T, TComparer>(this RedBlackNodeCollectionOperations<T> ops, ref RedBlackNode<T> root, T value, in TComparer comparer) where TComparer : struct, IComparer<T> {
         var res = ops.TryAdd(root, value, in comparer);
         root = res.NewRoot;
         return res.Success;
      }

      public static bool TryAdd<T, TComparer>(this RedBlackNodeCollectionOperations<T, TComparer> ops, ref RedBlackNode<T> root, T value) where TComparer : struct, IComparer<T> {
         var res = ops.TryAdd(root, value);
         root = res.NewRoot;
         return res.Success;
      }

      public static bool TryAdd<T, TComparer>(this RedBlackNodeCollectionOperations<T> ops, ref RedBlackNode<T> root, T value, out RedBlackNode<T> newNode, in TComparer comparer) where TComparer : struct, IComparer<T> {
         var res = ops.TryAdd(root, value, in comparer);
         root = res.NewRoot;
         newNode = res.Node;
         return res.Success;
      }

      public static bool TryAdd<T, TComparer>(this RedBlackNodeCollectionOperations<T, TComparer> ops, ref RedBlackNode<T> root, T value, out RedBlackNode<T> newNode) where TComparer : struct, IComparer<T> {
         var res = ops.TryAdd(root, value);
         root = res.NewRoot;
         newNode = res.Node;
         return res.Success;
      }

      public static RedBlackNode<T> AddOrThrow<T, TComparer>(this RedBlackNodeCollectionOperations<T> ops, RedBlackNode<T> root, T value, in TComparer comparer) where TComparer : struct, IComparer<T> {
         var res = ops.TryAdd(root, value, in comparer);
         res.Success.AssertIsTrue();
         return res.NewRoot;
      }

      public static void AddOrThrow<T, TComparer>(this RedBlackNodeCollectionOperations<T> ops, ref RedBlackNode<T> root, T value, in TComparer comparer) where TComparer : struct, IComparer<T> {
         var res = ops.TryAdd(root, value, in comparer);
         res.Success.AssertIsTrue();
         root = res.NewRoot;
      }

      public static RedBlackNode<T> AddOrThrow<T, TComparer>(this RedBlackNodeCollectionOperations<T, TComparer> ops, RedBlackNode<T> root, T value) where TComparer : struct, IComparer<T> {
         var res = ops.TryAdd(root, value);
         res.Success.AssertIsTrue();
         return res.NewRoot;
      }
      public static void AddOrThrow<T, TComparer>(this RedBlackNodeCollectionOperations<T, TComparer> ops, ref RedBlackNode<T> root, T value) where TComparer : struct, IComparer<T> {
         var res = ops.TryAdd(root, value);
         res.Success.AssertIsTrue();
         root = res.NewRoot;
      }

      public static RedBlackNode<T> AddOrThrow<T, TComparer>(this RedBlackNodeCollectionOperations<T> ops, RedBlackNode<T> root, T value, out RedBlackNode<T> newNode, in TComparer comparer) where TComparer : struct, IComparer<T> {
         var res = ops.TryAdd(root, value, in comparer);
         res.Success.AssertIsTrue();
         newNode = res.Node;
         return res.NewRoot;
      }

      public static void AddOrThrow<T, TComparer>(this RedBlackNodeCollectionOperations<T> ops, ref RedBlackNode<T> root, T value, out RedBlackNode<T> newNode, in TComparer comparer) where TComparer : struct, IComparer<T> {
         var res = ops.TryAdd(root, value, in comparer);
         res.Success.AssertIsTrue();
         newNode = res.Node;
         root = res.NewRoot;
      }

      public static RedBlackNode<T> AddOrThrow<T, TComparer>(this RedBlackNodeCollectionOperations<T, TComparer> ops, RedBlackNode<T> root, T value, out RedBlackNode<T> newNode) where TComparer : struct, IComparer<T> {
         var res = ops.TryAdd(root, value);
         res.Success.AssertIsTrue();
         newNode = res.Node;
         return res.NewRoot;
      }

      public static void AddOrThrow<T, TComparer>(this RedBlackNodeCollectionOperations<T, TComparer> ops, ref RedBlackNode<T> root, T value, out RedBlackNode<T> newNode) where TComparer : struct, IComparer<T> {
         var res = ops.TryAdd(root, value);
         res.Success.AssertIsTrue();
         newNode = res.Node;
         root = res.NewRoot;
      }

      public static bool TryRemove<T, TComparer>(this RedBlackNodeCollectionOperations<T> ops, ref RedBlackNode<T> root, T value, in TComparer comparer) where TComparer : struct, IComparer<T> {
         var res = ops.TryRemove(root, value, in comparer);
         root = res.NewRoot;
         return res.Success;
      }

      public static bool TryRemove<T, TComparer>(this RedBlackNodeCollectionOperations<T, TComparer> ops, ref RedBlackNode<T> root, T value) where TComparer : struct, IComparer<T> {
         var res = ops.TryRemove(root, value);
         root = res.NewRoot;
         return res.Success;
      }
      public static bool TryRemove<T, TComparer>(this RedBlackNodeCollectionOperations<T> ops, ref RedBlackNode<T> root, T value, out RedBlackNode<T> removedNode, in TComparer comparer) where TComparer : struct, IComparer<T> {
         var res = ops.TryRemove(root, value, in comparer);
         root = res.NewRoot;
         removedNode = res.Node;
         return res.Success;
      }

      public static bool TryRemove<T, TComparer>(this RedBlackNodeCollectionOperations<T, TComparer> ops, ref RedBlackNode<T> root, T value, out RedBlackNode<T> removedNode) where TComparer : struct, IComparer<T> {
         var res = ops.TryRemove(root, value);
         root = res.NewRoot;
         removedNode = res.Node;
         return res.Success;
      }

      public static RedBlackNode<T> RemoveOrThrow<T, TComparer>(this RedBlackNodeCollectionOperations<T> ops, RedBlackNode<T> root, T value, in TComparer comparer) where TComparer : struct, IComparer<T> {
         var res = ops.TryRemove(root, value, in comparer);
         res.Success.AssertIsTrue();
         return res.NewRoot;
      }

      public static RedBlackNode<T> RemoveOrThrow<T, TComparer>(this RedBlackNodeCollectionOperations<T, TComparer> ops, RedBlackNode<T> root, T value) where TComparer : struct, IComparer<T> {
         var res = ops.TryRemove(root, value);
         res.Success.AssertIsTrue();
         return res.NewRoot;
      }
      public static RedBlackNode<T> RemoveOrThrow<T, TComparer>(this RedBlackNodeCollectionOperations<T> ops, RedBlackNode<T> root, T value, out RedBlackNode<T> removedNode, in TComparer comparer) where TComparer : struct, IComparer<T> {
         var res = ops.TryRemove(root, value, in comparer);
         res.Success.AssertIsTrue();
         removedNode = res.Node;
         return res.NewRoot;
      }

      public static RedBlackNode<T> RemoveOrThrow<T, TComparer>(this RedBlackNodeCollectionOperations<T, TComparer> ops, RedBlackNode<T> root, T value, out RedBlackNode<T> removedNode) where TComparer : struct, IComparer<T> {
         var res = ops.TryRemove(root, value);
         res.Success.AssertIsTrue();
         removedNode = res.Node;
         return res.NewRoot;
      }

      public static void RemoveNode<T>(this RedBlackNodeCollectionOperations<T> ops, ref RedBlackNode<T> root, RedBlackNode<T> removedNode) {
         root = ops.RemoveNode(root, removedNode);
      }

      public static RedBlackNode<T> AddSuccessor<T>(this RedBlackNodeCollectionOperations<T> ops, ref RedBlackNode<T> root, RedBlackNode<T> origin, T value) {
         root = ops.AddSuccessor(root, origin, value, out var newNode);
         return newNode;
      }

      public static void AddSuccessor<T>(this RedBlackNodeCollectionOperations<T> ops, ref RedBlackNode<T> root, RedBlackNode<T> origin, RedBlackNode<T> insertedNode) {
         root = ops.AddSuccessor(root, origin, insertedNode);
      }

      public static RedBlackNode<T> AddPredecessor<T>(this RedBlackNodeCollectionOperations<T> ops, ref RedBlackNode<T> root, RedBlackNode<T> origin, T value) {
         root = ops.AddPredecessor(root, origin, value, out var newNode);
         return newNode;
      }
      public static void AddPredecessor<T>(this RedBlackNodeCollectionOperations<T> ops, ref RedBlackNode<T> root, RedBlackNode<T> origin, RedBlackNode<T> insertedNode) {
         root = ops.AddPredecessor(root, origin, insertedNode);
      }

      public static RedBlackNode<T> AddPredecessorOrSuccessor<T>(this RedBlackNodeCollectionOperations<T> ops, ref RedBlackNode<T> root, RedBlackNode<T> origin, T value, int valueVsOriginComparison) {
         root = ops.AddPredecessorOrSuccessor(root, origin, value, out var newNode, valueVsOriginComparison);
         return newNode;
      }

      public static void AddPredecessorOrSuccessor<T>(this RedBlackNodeCollectionOperations<T> ops, ref RedBlackNode<T> root, RedBlackNode<T> origin, RedBlackNode<T> insertedNode, int valueVsOriginComparison) {
         root = ops.AddPredecessorOrSuccessor(root, origin, insertedNode, valueVsOriginComparison);
      }

      public static RedBlackNode<T> AddPredecessorOrSuccessor<T, TComparer>(this RedBlackNodeCollectionOperations<T, TComparer> ops, ref RedBlackNode<T> root, RedBlackNode<T> origin, T value, int valueVsOriginComparison) where TComparer : struct, IComparer<T> {
         root = ops.AddPredecessorOrSuccessor(root, origin, value, out var newNode);
         return newNode;
      }
      public static void AddPredecessorOrSuccessor<T, TComparer>(this RedBlackNodeCollectionOperations<T, TComparer> ops, ref RedBlackNode<T> root, RedBlackNode<T> origin, RedBlackNode<T> insertedNode, int valueVsOriginComparison) where TComparer : struct, IComparer<T> {
         root = ops.AddPredecessorOrSuccessor(root, origin, insertedNode, valueVsOriginComparison);
      }

      public static void ReplaceNode<T>(this RedBlackNodeCollectionOperations<T> ops, ref RedBlackNode<T> root, RedBlackNode<T> inTreeReplacee, RedBlackNode<T> outOfTreeReplacement) {
         var newRoot = ops.ReplaceNode(root, inTreeReplacee, outOfTreeReplacement);
         root = newRoot;
      }
   }
}
