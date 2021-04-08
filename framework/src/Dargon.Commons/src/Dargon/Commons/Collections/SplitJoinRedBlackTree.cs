using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dargon.Commons.Collections {
   /// <summary>
   /// Variation of MIT-licensed SortedSet from .NET Foundation
   ///
   /// Implements Split/Join as described in:
   ///    Guy E. Blelloch, Daniel Ferizovic, and Yihan Sun. 2016.
   ///    Just Join for Parallel Ordered Sets. In Proceedings of the 28th ACM Symposium
   ///    on Parallelism in Algorithms and Architectures (SPAA '16). Association for
   ///    Computing Machinery, New York, NY, USA, 253–264. DOI:https://doi.org/10.1145/2935764.2935768
   /// </summary>
   /// <typeparam name="T"></typeparam>
   /// <typeparam name="TComparer"></typeparam>
   public class SplitJoinRedBlackTree<T, TComparer> where TComparer : struct, IComparer<T> {
      public const int COUNT_UNKNOWN = -133337;
      private const bool kEnableDebugPrintSplitJoin = false;

      public TComparer Comparer;
      private Node root;
      private int count;
      private bool isInvalidated = false;

      public SplitJoinRedBlackTree(TComparer comparer) {
         this.Comparer = comparer;
      }

      public SplitJoinRedBlackTree(TComparer comparer, T[] initialValues, int initialValuesStartIndex = -1, int initialValuesLength = -1) : this(comparer) {
         InsertInOrderContiguous(initialValues, initialValuesStartIndex, initialValuesLength);
      }

      public bool HasRoot => root != null;

      public int Count => count.AssertIsGreaterThanOrEqualTo(0);

      public bool IsCountComputed => count >= 0;

      public void ComputeCount() {
         count = 0;
         ComputeCountHelper(root);
      }

      public void Clear() {
         count = 0;
         root = null;
         isInvalidated = false;
      }

      private void ComputeCountHelper(Node n) {
         if (n == null) return;
         count++;
         ComputeCountHelper(n.Left);
         ComputeCountHelper(n.Right);
      }

      private Node BuildRBTree(T[] values, int startIndex, int length) {
         startIndex.AssertIsGreaterThanOrEqualTo(0);
         startIndex.AssertIsLessThanOrEqualTo(values.Length);
         length.AssertIsGreaterThanOrEqualTo(0);
         (startIndex + length).AssertIsLessThanOrEqualTo(values.Length);

         if (length == 0) {
            return null;
         }

         // (assume depth of 1 node is 0)
         // 1 node  -> 0 // perfect tree
         // 2 nodes -> 1 // bottom row is red
         // 3 nodes -> 0 // perfect tree
         // 4 nodes -> 2 // bottom row is red
         // 5 nodes -> 2 // bottom row is red
         // 6 nodes -> 2 // bottom row is red
         // 7 nodes -> 0 // perfect tree
         var nPlus1 = length + 1;
         var nPlus1IsPowerOf2 = (nPlus1 & (nPlus1 - 1)) == 0;

         var redDepth =
            nPlus1IsPowerOf2
               ? -1
               : (int)Math.Floor(Math.Log2(length) + 1E-5 /* Epsilon maybe unnecessary */);

         return BuildRBTree(values, startIndex, startIndex + length - 1, 0, redDepth);
      }

      private Node BuildRBTree(T[] values, int lo, int hi, int depth, int redDepth) {
         if (lo > hi) {
            return null;
         }

         var mid = (lo + hi) / 2;
         var left = BuildRBTree(values, lo, mid - 1, depth + 1, redDepth);
         var right = BuildRBTree(values, mid + 1, hi, depth + 1, redDepth);

         return new Node(
            values[mid],
            depth == redDepth ? RedBlackColor.Red : RedBlackColor.Black,
            left,
            right,
            Node.GetBlackHeightOfParent(left));
      }

      public string ToGraphvizStringDebug() => ToGraphvizStringDebug(root);

      private string ToGraphvizStringDebug(Node initialNode) {
         var sb = new StringBuilder();
         sb.AppendLine("digraph RedBlackTree {");

         var nextNodeId = 0;

         var s = new Stack<(Node node, int nodeId, Node pred, int predId)>();
         if (initialNode != null) {
            s.Push((initialNode, nextNodeId++, null, -1));
         }

         while (s.Count > 0) {
            var (n, nid, p, pid) = s.Pop();
            var color = n.IsRed ? "red" : "black";
            sb.AppendLine($"  {nid} [label=\"h={n.BlackHeight}, v={n.Value}\" color=\"{color}\"]");

            if (pid >= 0) {
               sb.AppendLine($"  {pid} -> {nid}");
            }

            if (n.Left != null) s.Push((n.Left, nextNodeId++, n, nid));
            if (n.Right != null) s.Push((n.Right, nextNodeId++, n, nid));
         }

         sb.AppendLine("}");
         return sb.ToString();
      }

      public void AssertOnInvariants() {
         // Invariants:
         // (1) Each node is either red or black. - A given with our implementation
         // (2) All NIL leaves(figure 1) are considered black (Implicit NIL leaves with our implementation - nothing to compute)
         // (3) If a node is red, then both its children are black.
         // (4) Every path from a given node to any of its descendant NIL leaves goes through the same number of black nodes.
         // 
         // Additionally (for join/split support):
         // (5) Every node tracks its black height, the number of black nodes to its leaves
         if (root == null) return;

         root.Color.AssertEquals(RedBlackColor.Black);

         // rootToNodeBlacks: number of black nodes in [root, ..., node]
         var q = new Queue<(Node n, int rootToNodeBlacks, Node parent)>();
         q.Enqueue((root, root.IsBlack ? 1 : 0, null));

         var leaves = new List<Node>();
         var nodeToRootToNodeBlacks = new Dictionary<Node, int>();

         while (q.Count > 0) {
            var (n, rootToNodeBlacks, parentOrNull) = q.Dequeue();
            nodeToRootToNodeBlacks[n] = rootToNodeBlacks;

            // If a node is red, then both its children are black.
            // (alternatively, no red node has a red parent)
            if (parentOrNull is { } parent) {
               Assert.IsFalse(n.IsRed && parent.IsRed);

               // BST invariant
               var expectedCmpNe = n == parentOrNull.Left ? 1 : -1;
               Assert.NotEquals(expectedCmpNe, Math.Sign(Comparer.Compare(n.Value, parent.Value)));
            }

            if (n.Left == null && n.Right == null) {
               leaves.Add(n);
            } else {
               if (n.Left != null) q.Enqueue((n.Left, rootToNodeBlacks + (n.Left.IsBlack ? 1 : 0), n));
               if (n.Right != null) q.Enqueue((n.Right, rootToNodeBlacks + (n.Right.IsBlack ? 1 : 0), n));
            }
         }

         // Every path from a given node to any of its descendant NIL leaves goes through the same number of black nodes.
         var rootToLeafBlackCount = nodeToRootToNodeBlacks[leaves[0]];
         foreach (var leaf in leaves) {
            Assert.Equals(rootToLeafBlackCount, nodeToRootToNodeBlacks[leaf]);
         }

         // Console.WriteLine("Root->Leaf Blacks " + rootToLeafBlackCount);

         // Every node tracks its black height, the number of black nodes to its leaves
         foreach (var (node, rootToNodeBlacks) in nodeToRootToNodeBlacks) {
            // +1 because null children count as black nodes.
            var actualBlackHeight = rootToLeafBlackCount - rootToNodeBlacks + 1;
            Assert.Equals(node.BlackHeight, actualBlackHeight);
         }
      }

      private struct SearchResult {
         public Node Node, Parent, Grandparent, GreatGrandParent;
         public bool Match;
         public int ValueVsNodeComparison;
         public int ValueVsParentComparison;
      }

      /// <summary>
      /// Terminates either with:
      /// 1. Current is nonnull, Match = True
      /// 2. Current is null, Parent is where we'd do an insert.
      /// </summary>
      private SearchResult Search(T value, bool split4Nodes) {
         Node current = root,
            parent = null,
            grandparent = null,
            greatGrandparent = null;

         int valueVsCurrentComparison = -1;
         int valueVsParentComparison = -1;

         while (current != null) {
            valueVsCurrentComparison = Comparer.Compare(value, current.Value);
            if (valueVsCurrentComparison == 0) {
               break;
            }

            if (split4Nodes && current.Is4Node) {
               current.Split4Node();

               if (Node.IsNonNullRed(parent)) {
                  InsertionBalance(current, ref parent, grandparent, greatGrandparent);
               }
            }

            greatGrandparent = grandparent;
            grandparent = parent;
            parent = current;
            current = valueVsCurrentComparison < 0 ? current.Left : current.Right;

            valueVsParentComparison = valueVsCurrentComparison;
            valueVsCurrentComparison = -1;
         }

         return new SearchResult {
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
      private void InsertionBalance(Node current, ref Node parent, Node grandParent, Node greatGrandParent) {
         Debug.Assert(parent != null);
         Debug.Assert(grandParent != null);

         bool parentIsOnRight = grandParent.Right == parent;
         bool currentIsOnRight = parent.Right == current;

         Node newChildOfGreatGrandParent;
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

         ReplaceChildOrRoot(greatGrandParent, grandParent, newChildOfGreatGrandParent);
      }

      /// <summary>
      /// Replaces the child of a parent node, or replaces the root if the parent is <c>null</c>.
      /// </summary>
      /// <param name="parent">The (possibly <c>null</c>) parent.</param>
      /// <param name="child">The child node to replace.</param>
      /// <param name="newChild">The node to replace <paramref name="child"/> with.</param>
      private void ReplaceChildOrRoot(Node parent, Node child, Node newChild) {
         if (parent != null) {
            parent.ReplaceChild(child, newChild);
         } else {
            root = newChild;
         }
      }

      /// <summary>
      /// Replaces the matching node with its successor.
      /// </summary>
      private void ReplaceNode(Node match, Node parentOfMatch, Node successor, Node parentOfSuccessor) {
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
               successor.Right = match.Right;
            }

            successor.Left = match.Left;
         }

         if (successor != null) {
            successor.Color = match.Color;
         }

         ReplaceChildOrRoot(parentOfMatch, match, successor!);

         if (successor != null) {
            successor.BlackHeight = Node.GetBlackHeightOfParent(successor.Left);
         }

         if (parentOfMatch != null) {
            parentOfMatch.BlackHeight = Node.GetBlackHeightOfParent(successor);
         }
      }

      public bool AddOrThrow(T value) => TryAdd(value).AssertIsTrue();

      public bool TryAdd(T value) {
         AssertIsNotInvalidated();

         if (root == null) {
            root = new Node(value, RedBlackColor.Black);
            return true;
         }

         var search = Search(value, true);

         if (search.Match) {
            root.Color = RedBlackColor.Black;
            return false;
         }

         var node = new Node(value, RedBlackColor.Red);
         var parent = search.Parent;
         if (search.ValueVsParentComparison > 0) {
            parent.Right = node;
         } else {
            parent.Left = node;
         }

         if (parent.IsRed) {
            InsertionBalance(node, ref parent, search.Grandparent, search.GreatGrandParent);
         }

         root.Color = RedBlackColor.Black;
         count++;
         return true;
      }

      public bool Remove(T item) => DoRemove(item);

      internal bool DoRemove(T item) {
         AssertIsNotInvalidated();

         if (root == null) {
            return false;
         }

         // Search for a node and then find its successor.
         // Then copy the item from the successor to the matching node, and delete the successor.
         // If a node doesn't have a successor, we can replace it with its left child (if not empty),
         // or delete the matching node.
         //
         // In top-down implementation, it is important to make sure the node to be deleted is not a 2-node.
         // Following code will make sure the node on the path is not a 2-node.

         Node current = root;
         Node parent = null;
         Node grandParent = null;
         Node match = null;
         Node parentOfMatch = null;
         bool foundMatch = false;
         while (current != null) {
            if (current.Is2Node) {
               // Fix up 2-node
               if (parent == null) {
                  // `current` is the root. Mark it red.
                  current.Color = RedBlackColor.Red;
               } else {
                  Node sibling = parent.GetSibling(current);
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
                     ReplaceChildOrRoot(grandParent, parent, sibling);
                     // `sibling` will become the grandparent of `current`.
                     grandParent = sibling;
                     if (parent == match) {
                        parentOfMatch = sibling;
                     }

                     sibling = parent.GetSibling(current);
                  }

                  Debug.Assert(Node.IsNonNullBlack(sibling));

                  if (sibling.Is2Node) {
                     parent.Merge2Nodes();
                  } else {
                     // `current` is a 2-node and `sibling` is either a 3-node or a 4-node.
                     // We can change the color of `current` to red by some rotation.
                     Node newGrandParent = parent.Rotate(parent.GetRotation(current, sibling))!;

                     newGrandParent.Color = parent.Color;
                     parent.Color = RedBlackColor.Black;
                     current.Color = RedBlackColor.Red;

                     parent.BlackHeight = Node.GetBlackHeightOfParent(current);
                     newGrandParent.BlackHeight = Node.GetBlackHeightOfParent(parent);

                     ReplaceChildOrRoot(grandParent, parent, newGrandParent);
                     if (parent == match) {
                        parentOfMatch = newGrandParent;
                     }

                     grandParent = newGrandParent;
                  }
               }
            }

            // We don't need to compare after we find the match.
            int order = foundMatch ? -1 : Comparer.Compare(item, current.Value);
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
            ReplaceNode(match, parentOfMatch!, parent!, grandParent!);
            --count;
         }

         if (root != null) {
            root.Color = RedBlackColor.Black;
         }

         return foundMatch;
      }

      private static Node JoinRightRB(Node left, T k, Node right) {
         if (Node.GetBlackHeight(left) == Node.GetBlackHeight(right)) {
            return new Node(k, RedBlackColor.Red) {
               Left = left,
               Right = right,
               BlackHeight = Node.GetBlackHeightOfParent(left),
            };
         }

         var tprime = left;
         tprime.Right = JoinRightRB(left.Right, k, right);
         if (tprime.Color == RedBlackColor.Black && Node.IsNonNullRed(tprime.Right) && Node.IsNonNullRed(tprime.Right.Right)) {
            tprime.Right.Right.Color = RedBlackColor.Black;

            var res = tprime.RotateLeft();
            res.BlackHeight = Node.GetBlackHeightOfParent(res.Left);
            return res;
         }

         return tprime;
      }

      private static Node JoinLeftRB(Node left, T k, Node right) {
         if (Node.GetBlackHeight(left) == Node.GetBlackHeight(right)) {
            return new Node(k, RedBlackColor.Red) {
               Left = left,
               Right = right,
               BlackHeight = Node.GetBlackHeightOfParent(right),
            };
         }

         var tprime = right;
         tprime.Left = JoinLeftRB(left, k, right.Left);
         if (tprime.Color == RedBlackColor.Black && Node.IsNonNullRed(tprime.Left) && Node.IsNonNullRed(tprime.Left.Left)) {
            tprime.Left.Left.Color = RedBlackColor.Black;

            var res = tprime.RotateRight();
            res.BlackHeight = Node.GetBlackHeightOfParent(res.Right);
            return res;
         }

         return tprime;
      }

      private static Node JoinRB(Node left, T k, Node right) {
         Node res;
         if (Node.GetBlackHeight(left) > Node.GetBlackHeight(right)) {
            res = JoinRightRB(left, k, right);
         } else if (Node.GetBlackHeight(left) < Node.GetBlackHeight(right)) {
            res = JoinLeftRB(left, k, right);
         } else if (Node.IsNullOrBlack(left) && Node.IsNullOrBlack(right)) {
            res = new Node(k, RedBlackColor.Red, left, right, Node.GetBlackHeightOfParent(left));
         } else {
            res = new Node(k, RedBlackColor.Black, left, right, Node.GetBlackHeightOfParent(left));
         }

         return FixRedJoinRoot(res);
      }

      private static Node FixRedJoinRoot(Node node) {
         if (node == null) {
            return null;
         }

         if (!node.IsRed) {
            return node;
         }

         var leftHeight = Node.GetBlackHeight(node.Left);
         var rightHeight = Node.GetBlackHeight(node.Right);

         if (leftHeight == rightHeight) {
            node.Color = RedBlackColor.Black;
            return node;
         } else if (leftHeight < rightHeight) {
            var res = node.RotateLeft();
            res.Color = RedBlackColor.Black;
            res.Left.BlackHeight = Node.GetBlackHeightOfParent(res.Left.Right);
            res.BlackHeight = Node.GetBlackHeightOfParent(res.Left);
            return res;
         } else {
            var res = node.RotateRight();
            res.Color = RedBlackColor.Black;
            res.Right.BlackHeight = Node.GetBlackHeightOfParent(res.Right.Left);
            res.BlackHeight = Node.GetBlackHeightOfParent(res.Right);
            return res;
         }
      }

      private (bool success, Node left, Node right) TrySplit(Node node, T k) {
         if (node == null) {
            return (false, null, null);
         } else {
            var cmp = Comparer.Compare(node.Value, k);
            // Console.WriteLine("cmp " + node.Value + " vs " + k + " " + cmp);
            if (cmp == 0) {
               return (true, FixRedJoinRoot(node.Left), FixRedJoinRoot(node.Right));
            } else if (cmp > 0) {
               var (b, ll, lr) = TrySplit(node.Left, k);
               return (b, ll, JoinRB(lr, node.Value, FixRedJoinRoot(node.Right)));
            } else {
               var (b, rl, rr) = TrySplit(node.Right, k);
               return (b, JoinRB(FixRedJoinRoot(node.Left), node.Value, rl), rr);
            }
         }
      }

      private (Node, T) SplitLast(Node node) {
         if (node.Right == null) {
            return (node.Left, node.Value);
         } else {
            var (tprime, kprime) = SplitLast(node.Right);
            if (kEnableDebugPrintSplitJoin) Console.WriteLine("Split yields " + kprime);
            if (kEnableDebugPrintSplitJoin) Console.WriteLine("Split joinrb input " + ToGraphvizStringDebug(node.Left));
            if (kEnableDebugPrintSplitJoin) Console.WriteLine("Split joinrb input " + node.Value);
            if (kEnableDebugPrintSplitJoin) Console.WriteLine("Split joinrb input " + ToGraphvizStringDebug(tprime));
            var splitTree = JoinRB(node.Left, node.Value, tprime);
            if (kEnableDebugPrintSplitJoin) Console.WriteLine("Split joinrb out: " + ToGraphvizStringDebug(splitTree));
            return (splitTree, kprime);
         }
      }

      private (Node, T) SplitFirst(Node node) {
         if (node.Left == null) {
            return (node.Right, node.Value);
         } else {
            var (tprime, kprime) = SplitFirst(node.Left);
            var splitTree = JoinRB(tprime, node.Value, node.Right);
            return (splitTree, kprime);
         }
      }

      private Node Join2(Node tl, Node tr) {
         if (tl == null) return tr;
         if (tr == null) return tl;

         var (tlprime, k) = SplitLast(tl);
         return JoinRB(tlprime, k, tr);
      }

      public SplitJoinRedBlackTree<T, TComparer> DestructiveJoin(SplitJoinRedBlackTree<T, TComparer> right) {
         AssertIsNotInvalidated();

         var res = new SplitJoinRedBlackTree<T, TComparer>(Comparer) {
            count = count == COUNT_UNKNOWN || right.count == COUNT_UNKNOWN
               ? COUNT_UNKNOWN
               : count + right.count,
            root = Join2(root, right.root),
         };
         MarkInvalidated();
         right.MarkInvalidated();
         return res;
      }

      public SplitJoinRedBlackTree<T, TComparer> DestructiveJoin(T k, SplitJoinRedBlackTree<T, TComparer> right) {
         AssertIsNotInvalidated();

         var res = new SplitJoinRedBlackTree<T, TComparer>(Comparer) {
            count = count == COUNT_UNKNOWN || right.count == COUNT_UNKNOWN
               ? COUNT_UNKNOWN
               : count + 1 + right.count,
            root = JoinRB(root, k, right.root),
         };
         MarkInvalidated();
         right.MarkInvalidated();
         return res;
      }

      public struct SplitResult {
         public SplitJoinRedBlackTree<T, TComparer> Left;
         public SplitJoinRedBlackTree<T, TComparer> Right;
         public T Splitter;
         public bool SplitterFound;
      }

      public SplitResult DestructiveSplit(T splitter) {
         AssertIsNotInvalidated();

         var (found, left, right) = TrySplit(root, splitter);
         MarkInvalidated();

         return new SplitResult {
            Left = new SplitJoinRedBlackTree<T, TComparer>(Comparer) {
               count = COUNT_UNKNOWN,
               root = left,
            },
            Right = new SplitJoinRedBlackTree<T, TComparer>(Comparer) {
               count = COUNT_UNKNOWN,
               root = right,
            },
            Splitter = splitter,
            SplitterFound = found,
         };
      }

      private void MarkInvalidated() {
         isInvalidated = true;
         root = null;
      }

      private void AssertIsNotInvalidated() {
         Assert.IsFalse(isInvalidated, "Tree is invalidated (e.g. in response to a Join/Split operation). Clear before using again.");
      }

      public T[] ToArray() {
         if (!IsCountComputed) {
            // this is an O(N) operation anyway, so no biggie.
            ComputeCount();
         }

         var res = new T[count];
         var nextIndex = 0;
         ToArrayHelper(root, res, ref nextIndex);
         Assert.Equals(nextIndex, res.Length);
         return res;
      }

      private void ToArrayHelper(Node current, T[] res, ref int nextIndex) {
         if (current == null) return;
         ToArrayHelper(current.Left, res, ref nextIndex);
         res[nextIndex++] = current.Value;
         ToArrayHelper(current.Right, res, ref nextIndex);
      }

      public class Node {
         public Node Parent;
         public Node Left, Right;
         public T Value;
         public RedBlackColor Color;

         // The black height of a black leaf is 0.
         // The black height of the root of a 2-node tree of black nodes is 1.
         public int BlackHeight;

         public Node(T value, RedBlackColor color) {
            Value = value;
            Color = color;
            BlackHeight = 1; // a node without children has implicit NIL leaves.
         }

         public Node(T value, RedBlackColor color, Node left, Node right, int blackHeight) {
            Value = value;
            Color = color;
            Left = left;
            Right = right;
            BlackHeight = blackHeight;

            Left.Parent = this;
            Right.Parent = this;
         }

         public bool IsRed => Color == RedBlackColor.Red;
         public bool IsBlack => Color == RedBlackColor.Black;


         public static bool IsNonNullBlack(Node node) => node != null && node.IsBlack;

         public static bool IsNonNullRed(Node node) => node != null && node.IsRed;
         public static bool IsNonNullLeaf(Node node) => node != null && node.IsLeaf;

         public static bool IsNullOrBlack(Node node) => node == null || node.IsBlack;

         public bool Is4Node => IsNonNullRed(Left) && IsNonNullRed(Right);
         public bool Is2Node => IsBlack && IsNullOrBlack(Left) && IsNullOrBlack(Right);

         public bool IsLeaf => Left == null && Right == null;

         /// <summary>
         /// Gets the rotation this node should undergo during a removal.
         /// </summary>
         public TreeRotation GetRotation(Node current, Node sibling) {
            Debug.Assert(IsNonNullRed(sibling.Left) || IsNonNullRed(sibling.Right));
#if DEBUG
                Debug.Assert(HasChildren(current, sibling));
#endif

            bool currentIsLeftChild = Left == current;
            return IsNonNullRed(sibling.Left) ? (currentIsLeftChild ? TreeRotation.RightLeft : TreeRotation.Right) : (currentIsLeftChild ? TreeRotation.Left : TreeRotation.LeftRight);
         }

         /// <summary>
         /// Does a rotation on this tree. May change the color of a grandchild from red to black.
         /// </summary>
         public Node Rotate(TreeRotation rotation) {
            Node removeRed;
            switch (rotation) {
               case TreeRotation.Right:
                  removeRed = Left!.Left!;
                  Debug.Assert(removeRed.IsRed);
                  removeRed.Color = RedBlackColor.Black;
                  return RotateRight();
               case TreeRotation.Left:
                  removeRed = Right!.Right!;
                  Debug.Assert(removeRed.IsRed);
                  removeRed.Color = RedBlackColor.Black;
                  return RotateLeft();
               case TreeRotation.RightLeft:
                  Debug.Assert(Right!.Left!.IsRed);
                  return RotateRightLeft();
               case TreeRotation.LeftRight:
                  Debug.Assert(Left!.Right!.IsRed);
                  return RotateLeftRight();
               default:
                  throw new InvalidOperationException($"{nameof(rotation)}: {rotation} is not a defined {nameof(TreeRotation)} value.");
            }
         }

         private const bool kEnableDebugPrint = false;

         /// <summary>
         /// Does a left rotation on this tree, making this node the new left child of the current right child.
         /// </summary>
         public Node RotateLeft() {
            if (kEnableDebugPrint) Console.WriteLine("rotL");

            Node child = Right!;
            Right = child.Left;
            child.Left = this;

            return child;
         }

         public static int GetBlackHeightOfParent(Node n) {
            if (n == null) return 1;
            return n.BlackHeight + (n.IsBlack ? 1 : 0);
         }

         public static int GetBlackHeight(Node n) {
            if (n == null) return 0;
            return n.BlackHeight;
         }

         /// <summary>
         /// Does a left-right rotation on this tree. The left child is rotated left, then this node is rotated right.
         /// </summary>
         public Node RotateLeftRight() {
            if (kEnableDebugPrint) Console.WriteLine("rotLR");
            Node child = Left!;
            Node grandChild = child.Right!;

            Left = grandChild.Right;
            grandChild.Right = this;
            child.Right = grandChild.Left;
            grandChild.Left = child;

            return grandChild;
         }

         /// <summary>
         /// Does a right rotation on this tree, making this node the new right child of the current left child.
         /// </summary>
         public Node RotateRight() {
            if (kEnableDebugPrint) Console.WriteLine("rotR");
            Node child = Left!;
            Left = child.Right;
            child.Right = this;

            return child;
         }

         /// <summary>
         /// Does a right-left rotation on this tree. The right child is rotated right, then this node is rotated left.
         /// </summary>
         public Node RotateRightLeft() {
            if (kEnableDebugPrint) Console.WriteLine("rotRL");
            Node child = Right!;
            Node grandChild = child.Left!;

            Right = grandChild.Left;
            grandChild.Left = this;
            child.Left = grandChild.Right;
            grandChild.Right = child;

            return grandChild;
         }

         /// <summary>
         /// Replaces a child of this node with a new node.
         /// </summary>
         /// <param name="child">The child to replace.</param>
         /// <param name="newChild">The node to replace <paramref name="child"/> with.</param>
         public void ReplaceChild(Node child, Node newChild) {
            if (Left == child) {
               Left = newChild;
            } else {
               Debug.Assert(Right == child);
               Right = newChild;
            }
         }

         public Node GetSibling(Node node) {
            Debug.Assert(node != null);
            Debug.Assert(node == Left ^ node == Right);

            return node == Left ? Right! : Left!;
         }

         public void Split4Node() {
            Color = RedBlackColor.Red;
            BlackHeight++;
            Left.Color = RedBlackColor.Black;
            Right.Color = RedBlackColor.Black;
         }

         /// <summary>
         /// Combines two 2-nodes into a 4-node.
         /// </summary>
         public void Merge2Nodes() {
            Debug.Assert(IsRed);
            Debug.Assert(Left!.Is2Node);
            Debug.Assert(Right!.Is2Node);

            Color = RedBlackColor.Black;
            BlackHeight--;
            Left.Color = RedBlackColor.Red;
            Right.Color = RedBlackColor.Red;
         }

         public (Node, (T, RedBlackColor), Node) Expose() {
            return (Left, (Value, Color), Right);
         }
      }

      public void InsertInOrderContiguous(T[] values) {
         InsertInOrderContiguous(values, 0, values.Length);
      }

      public void InsertInOrderContiguous(T[] values, int index, int length) {
         if (length == 0) {
            return;
         } else if (length == 1) {
            // note for length >= 2, we can't simulate this invoke with tryadd
            // in the case where inserted values are equal according to cmp
            TryAdd(values[index]);
            return;
         }

         if (count == 0) {
            root = BuildRBTree(values, index, length);
            count = values.Length;
            return;
         }
         
         var (v0Found, left, temp) = TrySplit(root, values[index]);
         var (vfFound, center, right) = TrySplit(temp, values[index + length - 1]);

         if (center != null) {
            throw new InvalidOperationException("The inserted sequence would not have been in-order contiguous.");
         }

         // centerTree = BuildRBTree(values, index + 1, length - 2);

         // var leftCenterJoin = JoinRB(leftTree, values[index], centerTree);
         // root = JoinRB(leftCenterJoin, values[index + length - 1], rightTree);
         // count += length - (v0Found ? 1 : 0) - (vfFound ? 1 : 0);
      }

      public enum RedBlackColor {
         Black,
         Red,
      }

      public enum TreeRotation : byte {
         Left,
         LeftRight,
         Right,
         RightLeft
      }
   }
}
