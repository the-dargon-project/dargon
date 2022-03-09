#define PFOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NMockito;
using Xunit;
using static NMockito.NMockitoStatics;

namespace Dargon.Commons.Collections.RedBlackTrees {
   public class RedBlackTreeCollectionOperationsTests {
      private struct IntComparer : IComparer<int> {
         public int Compare(int x, int y) {
            return x - y;
         }
      }

      [Fact]
      public void RedBlackTree_GetPredecessorSuccessorFT() {
         foreach (var treeSize in new[] { 0, 1, 2, 3, 7, 100 }) {
            var ops = new RedBlackNodeCollectionOperations<int, IntComparer>(new IntComparer());
            var root = ops.Treeify(Arrays.Create(treeSize, i => i));
            var nodesInOrder = ops.ToNodeArray(root);
            for (var i = 0; i < nodesInOrder.Length; i++) {
               var node = nodesInOrder[i];
               var pred = ops.GetPredecessorOrNull(node);
               var succ = ops.GetSuccessorOrNull(node);

               if (i == 0) {
                  pred.AssertEquals(null);
               } else {
                  pred.AssertEquals(nodesInOrder[i - 1]);
               }

               if (i + 1 == nodesInOrder.Length) {
                  succ.AssertEquals(null);
               } else {
                  succ.AssertEquals(nodesInOrder[i + 1]);
               }
            }
         }
      }

      [Fact]
      public void RedBlackTree_AddSuccessorPredecessorFT() {
         var configs = new List<(int niters, int numInitialNodesMax, int cubeRootNumAdds)> {
            (1000, 10, 10),
            (1000, 100, 4),
         };

         foreach (var (niters, numInitialNodesMax, cubeRootNumAdds) in configs) {
            Console.WriteLine($"{nameof(RedBlackTree_AddSuccessorPredecessorFT)} with {niters} {numInitialNodesMax} {cubeRootNumAdds}");
#if PFOR
            Parallel.For(0, niters, iteration => {
#else
            for (var iteration = 0; iteration <= niters; iteration++) {
               // Console.WriteLine("Iteration " + iteration);
#endif
               var r = new Random(iteration);

               var ops = new RedBlackNodeCollectionOperations<int, IntComparer>(new IntComparer());
               var numInitialNodes = r.Next(numInitialNodesMax) + 1;
               var root = ops.CreateEmptyTree();
               var nodes = new List<RedBlackNode<int>>();
               for (var i = 0; i < numInitialNodes; i++) {
                  ops.AddOrThrow(ref root, i * 1000000, out var node);
                  nodes.Add(node);
               }

               ops.VerifyInvariants(root);

               nodes = nodes.ToArray().ShuffleInPlace(new Random(r.Next())).ToList();

               foreach (var node in nodes) {
                  var nadds = r.Next(cubeRootNumAdds);
                  nadds = nadds * nadds * nadds;

                  if (r.Next(2) == 0) {
                     for (var offset = -nadds; offset < 0; offset++) {
                        var v = node.Value + offset;
                        var n = RedBlackNode.CreateForInsertion(v);
                        ops.AddPredecessor(ref root, node, n);
                        n.BlackHeight.AssertIsLessThanOrEqualTo(2);
                        // ops.VerifyInvariants(root);
                     }

                     for (var offset = nadds; offset > 0; offset--) {
                        var v = node.Value + offset;
                        var n = RedBlackNode.CreateForInsertion(v);
                        ops.AddSuccessor(ref root, node, n);
                        n.BlackHeight.AssertIsLessThanOrEqualTo(2);
                        // ops.VerifyInvariants(root);
                     }

                     ops.VerifyInvariants(root);
                  } else {
                     var cur = node;
                     for (var i = 0; i < nadds; i++) {
                        ops.AddPredecessor(ref root, cur, cur.Value - 1, out var next);
                        cur = next;
                        next.BlackHeight.AssertIsLessThanOrEqualTo(2);
                        // ops.VerifyInvariants(root);
                     }

                     cur = node;
                     for (var i = 1; i < nadds; i++) {
                        ops.AddSuccessor(ref root, cur, cur.Value + 1, out var next);
                        cur = next;
                        next.BlackHeight.AssertIsLessThanOrEqualTo(2);
                        // ops.VerifyInvariants(root);
                     }

                     ops.VerifyInvariants(root);
                  }
               }

#if PFOR
            });
#else
            }
#endif
         }
      }

      [Fact]
      public void RedBlackTree_AddContiguousFT() {
         var configs = new List<(int, int, int)> {
            (10000, 0, 0),
            (10000, 3, 0),
            (10000, 0, 3),
            (10000, 3, 3),
            (10000, 7, 7),
            (100000, 127, 127),
            (100000, 7, 1023),
            (100000, 1023, 1023),
         };

         var ops = new RedBlackNodeCollectionOperations<int, IntComparer>(new IntComparer());

         foreach (var (niters, treeSizeMax, insertionSizeMax) in configs) {
            Console.WriteLine($"Running AddContiguous Tests for n<={treeSizeMax} k<={insertionSizeMax} niters={niters}");
#if PFOR
            Parallel.For(0, niters, i => {
#else
            for (var i = 0; i < niters; i++) {
#endif
               if ((i % (niters / 10)) == 0) {
                  Console.WriteLine($"Iteration {i} of {niters}");
               }

               var r = new Random(i);
               var treeSize = r.Next(treeSizeMax + 1);

               var treeValues = Arrays.Create(treeSize, idx => idx * 10000);
               var treeValuesShuffled = treeValues.ToArray().ShuffleInPlace(r);

               var insertionBase = treeValues.Length == 0
                  ? 0 // target tree is empty
                  : r.Next(treeSize + 1) * 10000 - 5000;

               var insertionSize = r.Next(insertionSizeMax + 1);
               var insertionValues = Arrays.Create(insertionSize, idx => insertionBase + idx + 1);

               var tree = ops.CreateEmptyTree();
               foreach (var v in treeValuesShuffled) {
                  tree = ops.AddOrThrow(tree, v);
               }

               Assert.Equals(treeSize, ops.CountNodes(tree));

               var res = ops.AddContiguous(tree, insertionValues);
               res.Success.AssertIsTrue();

               var results = ops.ToArray(res.NewRoot);
               Assert.Equals(treeSize + insertionSize, results.Length);
               ops.VerifyInvariants(res.NewRoot);
#if PFOR
            });
#else
            }
#endif
         }
      }

      [Fact]
      public void RedBlackTree_SplitJoinFT() {
         const bool kDebugEnabled = false;

         var configs = new List<(int niters, int initialTreeSize)> {
            (8, 0),
            (8, 1),
            (100, 3),
            (100, 7),
            (1000, 31),
            (300, 127),
            (100, 1023),
            (500, 2047),
         };

         foreach (var (niters, initialTreeSize) in configs) {
            Console.WriteLine("Running split/join test for size " + initialTreeSize);
            for (var i = 0; i < niters; i++) {
               var r = new Random(i);
               var allValues = Arrays.Create(initialTreeSize * 2, i => i * 10);
               allValues.ShuffleInPlace(r);

               var values = allValues.SubArray(0, r.Next(initialTreeSize));
               Array.Sort(values);
               var treeOperations = new RedBlackNodeCollectionOperations<int, IntComparer>(new IntComparer());
               var tree = treeOperations.Treeify(values);

               // pick random index in values to split between
               // 0: [][values] split, split is before values[0]
               // 1: [][values] split, split is on values[0]
               // 2 * (values.Length - 1): split is before values[length - 1]
               // 2 * (values.Length - 1) + 1: split is on values[length - 1]
               // 2 * values.Length: split is after values[length - 1]
               var rand = r.Next(2 * values.Length + 1);
               var isElementMatch = rand % 2 == 1;
               var pivotIndex = rand / 2;
               var splitter =
                  values.Length == 0 ? 1337 /* no elements in tree */ :
                  pivotIndex == values.Length ? values[^1] + 5 /* after last node in tree */ :
                  isElementMatch ? values[pivotIndex] :
                  values[pivotIndex] - 5;

               if (kDebugEnabled) Console.WriteLine($"============ it {i}");
               if (kDebugEnabled) Console.WriteLine("Tree Values " + values.Join(","));
               if (kDebugEnabled) treeOperations.DumpToConsoleColored(tree);
               var res = treeOperations.TrySplit(tree, splitter);
               var left = res.Left;
               var right = res.Right;

               if (kDebugEnabled) Console.WriteLine($"Split by {splitter} (Exact Match: {isElementMatch})");
               if (kDebugEnabled) Console.WriteLine("Left " + treeOperations.ToArray(left).Join(","));
               if (kDebugEnabled) treeOperations.DumpToConsoleColored(left);
               if (kDebugEnabled) Console.WriteLine("Right " + treeOperations.ToArray(right).Join(","));
               if (kDebugEnabled) treeOperations.DumpToConsoleColored(right);

               Assert.Equals(isElementMatch, res.Match != null);
               Assert.Equals(values.SubArray(0, pivotIndex).Join(","), treeOperations.ToArray(left).Join(","));
               Assert.Equals(values.SubArray(pivotIndex + (isElementMatch ? 1 : 0)).Join(","), treeOperations.ToArray(right).Join(","));

               treeOperations.VerifyInvariants(left);
               treeOperations.VerifyInvariants(right);

               if (r.Next(5) <= 3) {
                  var joined =
                     isElementMatch
                        ? treeOperations.JoinRB(left, res.Match, right)
                        : treeOperations.Join2(left, right);

                  Assert.Equals(values.Join(","), treeOperations.ToArray(joined).Join(","));
               } else {
                  // try some random insertions into the trees
                  var leftValues = treeOperations.ToArray(left).ToHashSet();
                  var rightValues = treeOperations.ToArray(right).ToHashSet();
                  var valuesToInsert = allValues.SubArray(values.Length, r.Next(initialTreeSize / 2))
                                                .Concat(leftValues)
                                                .Concat(rightValues)
                                                .OrderBy(x => x).ToArray();
                  foreach (var v in valuesToInsert) {
                     Assert.Equals(!leftValues.Contains(v), treeOperations.TryAdd(ref left, v));
                     Assert.IsTrue(treeOperations.TryRemove(ref left, v));

                     Assert.Equals(!rightValues.Contains(v), treeOperations.TryAdd(ref right, v));
                     Assert.IsTrue(treeOperations.TryRemove(ref right, v));
                  }
               }
            }
         }
      }

      [Fact]
      public void RedBlackTree_AddRemoveFT() {
         bool kEnableDebugPrint = false;

         var configs = new List<(int niters, int elementCount, int verifyCadence)> {
            (100, 127, 1),
            (1000, 127, 10),
            (1000, 1023, 100),
         };

         foreach (var (niters, elementCount, verifyCadence) in configs) {
            var ops = new RedBlackNodeCollectionOperations<int, IntComparer>(new IntComparer());

            Parallel.For(0, niters, i => {
               RedBlackNode<int> root = null;
               if (i % (niters / 10) == 0) Console.WriteLine($"Add/Remove Test N = {elementCount} Iteration {i} of {niters}");

               var r = new Random(i);
               var allValues = Arrays.Create(elementCount, i => i);

               var randomizedAllValues = allValues.ToArray();
               randomizedAllValues.ShuffleInPlace(r);

               for (var index = 0; index < randomizedAllValues.Length; index++) {
                  var v = randomizedAllValues[index];
                  if (kEnableDebugPrint) Console.WriteLine("ADD " + v);
                  if (kEnableDebugPrint) Console.WriteLine(ops.ToGraphvizStringDebug(root));
                  var addResult = ops.TryAdd(root, v);
                  addResult.Success.AssertIsTrue();
                  addResult.Node.BlackHeight.AssertIsLessThanOrEqualTo(2);
                  root = addResult.NewRoot;

                  if (kEnableDebugPrint) Console.WriteLine(ops.ToGraphvizStringDebug(root));
                  if (index % verifyCadence == 0) {
                     ops.VerifyInvariants(root);
                  }
               }

               if (kEnableDebugPrint) Console.WriteLine("ADDS DONE");
               Assert.Equals(
                  ops.ToArray(root).Join(","),
                  allValues.Join(","));

               // if (kEnableDebugPrint) Console.WriteLine(ops.ToGraphvizStringDebug(root));
               randomizedAllValues.ShuffleInPlace(r);

               // Console.WriteLine("Removes:");
               for (var index = 0; index < randomizedAllValues.Length; index++) {
                  var v = randomizedAllValues[index];
                  var removeResult = ops.TryRemove(root, v);
                  removeResult.Success.AssertIsTrue();
                  root = removeResult.NewRoot;

                  // if (kEnableDebugPrint) Console.WriteLine(ops.ToGraphvizStringDebug(root));
                  if (index % verifyCadence == 0) {
                     ops.VerifyInvariants(root);
                  }
               }

               // Console.WriteLine(rb.ToGraphvizStringDebug());
            });
         }
      }

      [Fact]
      public void RedBlackTree_RemoveSpecificNodeFT() {
         bool kEnableDebugPrint = false;

         var configs = new List<(int niters, int elementCount)> {
            (1000, 3),
            (1000, 7),
            (1000, 15),
            (1000, 31),
            (1000, 63),
            (1000, 127),
            (100, 1023),
         };


         foreach (var (niters, elementCount) in configs) {
            var ops = new RedBlackNodeCollectionOperations<int, IntComparer>(new IntComparer());

            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
            Parallel.For(0, niters, parallelOptions, i => {
               RedBlackNode<int> root = null;
               if (i % (niters / 10) == 0) Console.WriteLine($"Remove Specific Node Test N = {elementCount} Iteration {i} of {niters}");

               var r = new Random(i);
               var allValues = Arrays.Create(elementCount, i => i);

               var randomizedAllValues = allValues.ToArray();
               randomizedAllValues.ShuffleInPlace(r);

               var valueToNode = new RedBlackNode<int>[elementCount];

               foreach (var value in randomizedAllValues) {
                  ops.AddOrThrow(ref root, value, out var newNode);
                  valueToNode[value] = newNode;
               }

               ops.VerifyInvariants(root);

               foreach (var value in randomizedAllValues) {
                  if (kEnableDebugPrint) Console.WriteLine("=====================================");
                  if (kEnableDebugPrint) Console.WriteLine("=====================================");
                  if (kEnableDebugPrint) Console.WriteLine("=====================================");
                  if (kEnableDebugPrint) Console.WriteLine("Remove Value " + value);
                  if (kEnableDebugPrint) ops.DumpToConsoleColored(root);
                  ops.RemoveNode(ref root, valueToNode[value]);
                  if (kEnableDebugPrint) Console.WriteLine("=====================================");
                  if (kEnableDebugPrint) Console.WriteLine("=====================================");
                  if (kEnableDebugPrint) ops.DumpToConsoleColored(root);
                  if (kEnableDebugPrint) Console.WriteLine("=====================================");
                  ops.VerifyInvariants(root);

               }

               root.AssertIsNull();
            });
         }
      }

      [Fact]
      public void RedBlackTree_SplitFirstLastFT() {
         var ops = new RedBlackNodeCollectionOperations<int, IntComparer>(new IntComparer());

         var configs = new List<(int treeMaxSizes, int niters)> {
            (1, 10000),
            (5, 10000),
            (15, 10000),
            (127, 10000),
         };
         foreach (var (treeMaxSize, niters) in configs) {
            Console.WriteLine($"Split Test TreeMaxSize {treeMaxSize} niters {niters}");
            for (var i = 0; i < niters; i++) {
               if (i % (niters / 10) == 0) Console.WriteLine($"Iteration {i} / {niters}");

               var r = new Random(i);
               var size = r.Next(treeMaxSize - 1) + 1;
               var allLeftValues = new List<int>(Enumerable.Range(0, size));

               var tree = ops.CreateEmptyTree();
               var lowest = ops.CreateEmptyTree();
               var highest = ops.CreateEmptyTree();
               foreach (var v in allLeftValues.Shuffle(r)) {
                  tree = ops.AddOrThrow(tree, v, out var node);
                  if (v == 0) lowest = node;
                  if (v == size - 1) highest = node;
               }

               ops.VerifyInvariants(tree);

               Assert.Equals(ops.ToArray(tree).Join(","), allLeftValues.Join(","));

               var op = r.Next(2);
               // Console.WriteLine($"==== it {i}, op {op}");
               switch (op) {
                  case 0:
                     // ops.DumpToConsoleColored(tree);
                     var (first, right) = ops.SplitFirst(tree);
                     // ops.DumpToConsoleColored(right);
                     Assert.Equals(first, lowest);
                     Assert.Equals(ops.ToArray(right).Join(","), allLeftValues.Skip(1).Join(","));
                     ops.VerifyInvariants(first);
                     ops.VerifyInvariants(right);
                     break;
                  case 1:
                     // ops.DumpToConsoleColored(tree);
                     var (left, last) = ops.SplitLast(tree);
                     // ops.DumpToConsoleColored(left);
                     Assert.Equals(last, highest);
                     Assert.Equals(ops.ToArray(left).Join(","), allLeftValues.Take(size - 1).Join(","));
                     ops.VerifyInvariants(left);
                     ops.VerifyInvariants(last);
                     break;
               }
            }
         }
      }

      [Fact]
      public void RedBlackTree_JoinFT() {
         var ops = new RedBlackNodeCollectionOperations<int, IntComparer>(new IntComparer());

         // var allLeftValues = new List<int>(Enumerable.Range(100000, 99999));
         // var allRightValues = new List<int>(Enumerable.Range(500000, 99999));

         var treeMaxSizes = new List<(int, int, int niters)> {
            (3, 3, 1000),
            (15, 15, 10000),
            (31, 3, 1000),
            (3, 31, 1000),
            (127, 127, 100000),
         };
         foreach (var (leftTreeSizeMax, rightTreeSizeMax, niters) in treeMaxSizes) {
            Console.WriteLine($"Join Test TreeSizeMaxes {leftTreeSizeMax} {rightTreeSizeMax}");
            Parallel.For(0, niters, i => {
               if (i % (niters / 10) == 0) Console.WriteLine($"Iteration {i} / {niters}");
               var r = new Random(i);
               var seed0 = r.Next();
               var seed1 = r.Next();

               var r0 = new Random(seed0);
               var leftCount = r0.Next(leftTreeSizeMax);
               var leftValues = Arrays.Create(leftCount, i => i + 100000);
               // var leftValues = allLeftValues.Shuffle(r0).Take(r0.Next(leftTreeSizeMax)).OrderBy(x => x).ToList();

               var r1 = new Random(seed1);
               var rightCount = r1.Next(rightTreeSizeMax);
               var rightValues = Arrays.Create(rightCount, i => i + 500000);
               // var rightValues = allRightValues.Shuffle(r1).Take(r1.Next(rightTreeSizeMax)).OrderBy(x => x).ToList();


               var left = ops.Treeify(leftValues);
               // Console.WriteLine("LEFT: " + left.ToGraphvizStringDebug());
               // ops.VerifyInvariants(left);

               var right = ops.Treeify(rightValues);
               // Console.WriteLine("RIGHT: " + right.ToGraphvizStringDebug());
               // ops.VerifyInvariants(right);

               var res = ops.Join2(left, right);
               // Console.WriteLine("JOIN: " + res.ToGraphvizStringDebug());
               ops.VerifyInvariants(res);

               // Console.WriteLine($"Joining {leftValues.Count} {rightValues.Count} yields {res.Count}");
               Assert.Equals(
                  leftValues.Concat(rightValues).Join(","),
                  ops.ToArray(res).Join(","));
            });
         }
      }

      [Fact]
      public void RedBlackTree_ReplaceNodeFT() {
         var ops = new RedBlackNodeCollectionOperations<int, IntComparer>(new IntComparer());

         var treeMaxSizes = new List<(int treeSizeMax, int niters)> {
            (3, 100),
            (15, 100),
            (31, 100),
            (127, 10),
         };
         foreach (var (treeSizeMax, niters) in treeMaxSizes) {
            Console.WriteLine($"Replace Node Test TreeSizeMax {treeSizeMax} niters {niters}");
            Parallel.For(0, niters, i => {
               var r = new Random(i);
               var seed0 = r.Next();
               var seed1 = r.Next();

               // Create a tree of even numbers.
               var values = Arrays.Create(new Random(seed0).Next(treeSizeMax), i => i * 2);
               var insertionOrder = values.ToArray().ShuffleInPlace(new Random(seed1));

               var root = ops.CreateEmptyTree();
               var nodes = new List<RedBlackNode<int>>();
               foreach (var x in insertionOrder) {
                  ops.AddOrThrow(ref root, x, out var node);
                  nodes.Add(node);
               }

               Assert.Equals(values.Join(","), ops.ToArray(root).Join(","));

               // Replace every node with one of higher value.
               foreach (var node in nodes) {
                  ops.ReplaceNode(ref root, node, RedBlackNode.CreateForInsertion(node.Value + 1));
               }

               Assert.Equals(values.Map(v => v + 1).Join(","), ops.ToArray(root).Join(","));
               ops.VerifyInvariants(root);
            });
         }
      }
   }
}
