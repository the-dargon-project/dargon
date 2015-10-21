using System;
using System.Collections.Generic;
using System.Linq;

namespace NMockito.Utilities {
   public interface CombinatorialSetGenerator {
      IReadOnlyList<int[]> GenerateCombinations(params int[] combinatorialSetLengths);
   }

   public class PairwiseCombinatorialSetGenerator : CombinatorialSetGenerator {
      public IReadOnlyList<int[]> GenerateCombinations(params int[] combinatorialSetLengths) {
         if (combinatorialSetLengths.Length == 1) {
            return GenerateSingleColumnSet(combinatorialSetLengths);
         } else if (combinatorialSetLengths.Length == 2) {
            return GenerateDoubleColumnSet(combinatorialSetLengths);
         } else {
            return GenerateMultiColumnSet(combinatorialSetLengths);
         }
      }

      private static IReadOnlyList<int[]> GenerateSingleColumnSet(int[] combinatorialSetLengths) {
         var result = new int[combinatorialSetLengths[0]][];
         for (var i = 0; i < result.Length; i++) {
            result[i] = new[] { i };
         }
         return result;
      }

      private static IReadOnlyList<int[]> GenerateDoubleColumnSet(int[] combinatorialSetLengths) {
         var a = combinatorialSetLengths[0];
         var b = combinatorialSetLengths[1];
         var result = new int[a * b][];
         int i = 0;
         for (var x = 0; x < a; x ++) {
            for (var y = 0; y < b; y++) {
               var row = new int[combinatorialSetLengths.Length];
               row[0] = x;
               row[1] = y;
               result[i] = row;
               i++;
            }
         }
         return result;
      }

      private static IReadOnlyList<int[]> GenerateMultiColumnSet(int[] combinatorialSetLengths) {
         var tests = new List<int[]>(GenerateDoubleColumnSet(combinatorialSetLengths));

         for (var addedColumnIndex = 2; addedColumnIndex < combinatorialSetLengths.Length; addedColumnIndex++) {
            var piTable = new PiTable(combinatorialSetLengths, addedColumnIndex);
            GenerateMultiColumnSet_ExpandToColumn(tests, piTable, addedColumnIndex, combinatorialSetLengths[addedColumnIndex]);
            GenerateMultiColumnSet_ExpandVertically(tests, piTable, addedColumnIndex, combinatorialSetLengths);
         }
         return tests;
      }

      private static void GenerateMultiColumnSet_ExpandToColumn(List<int[]> tests, PiTable piTable, int addedColumnIndex, int addedSetLength) {
         if (tests.Count <= addedSetLength) {
            for (var i = 0; i < tests.Count; i++) {
               piTable.FlagCoveredPairs(tests[i], i);
               tests[i][addedColumnIndex] = i;
            }
         } else {
            for (var i = 0; i < addedSetLength; i++) {
               piTable.FlagCoveredPairs(tests[i], i);
               tests[i][addedColumnIndex] = i;
            }
            for (var i = addedSetLength; i < tests.Count; i++) {
               var optimalJ = -1;
               var optimalJCoveredPairs = -1;
               for (var j = 0; j < addedSetLength; j++) {
                  var coveredPairs = piTable.PeekAdditionalCoveredPairs(tests[i], j);
                  if (coveredPairs > optimalJCoveredPairs) {
                     optimalJ = j;
                     optimalJCoveredPairs = coveredPairs;
                  }
               }
               piTable.FlagCoveredPairs(tests[i], optimalJ);
               tests[i][addedColumnIndex] = optimalJ;
            }
         }
      }

      private static void GenerateMultiColumnSet_ExpandVertically(List<int[]> tests, PiTable piTable, int addedColumnIndex, int[] combinatorialSetLengths) {
         List<int[]> testsPrime = new List<int[]>();
         foreach (var pair in piTable.EnumeratePairs()) {
            int oldColumnIndex = pair.Item1, oldColumnValue = pair.Item2, addedColumnValue = pair.Item3;
            bool rowUpdated = false;
            for (var i = 0; i < testsPrime.Count && !rowUpdated; i++) {
               var test = testsPrime[i];
               if (test[oldColumnIndex] == -1 && test[addedColumnIndex] == addedColumnValue) {
                  test[oldColumnIndex] = oldColumnValue;
                  rowUpdated = true;
               }
            }
            if (!rowUpdated) {
               var newTest = new int[combinatorialSetLengths.Length];
               for (var i = 0; i < addedColumnIndex; i++) {
                  newTest[i] = -1;
               }
               newTest[oldColumnIndex] = oldColumnValue;
               newTest[addedColumnIndex] = addedColumnValue;
               testsPrime.Add(newTest);
            }
         }
         var fillersCount = 0;
         for (var i = 0; i < testsPrime.Count; i++) {
            var test = testsPrime[i];
            for (var columnIndex = 0; columnIndex < addedColumnIndex; columnIndex++) {
               if (test[columnIndex] == -1) {
                  test[columnIndex] = fillersCount % combinatorialSetLengths[columnIndex];
                  fillersCount++;
               }
            }
            tests.Add(test);
         }
      }

      public class PiTable {
         private readonly PiColumn[] columns;

         public PiTable(int[] combinatorialSetLengths, int addedColumnIndex) {
            columns = new PiColumn[addedColumnIndex];
            int addedCombinatorialSetLength = combinatorialSetLengths[addedColumnIndex];
            for (var i = 0; i < addedColumnIndex; i++) {
               columns[i] = new PiColumn(i, combinatorialSetLengths[i], addedCombinatorialSetLength);
            }
         }

         public void FlagCoveredPairs(int[] test, int addedColumnValue) {
//            Trace.Assert(columns.Length == test.Length);
            for (var columnIndex = 0; columnIndex < columns.Length; columnIndex++) {
               columns[columnIndex].FlagCoveredPairs(test[columnIndex], addedColumnValue);
            }
         }

         public int PeekAdditionalCoveredPairs(int[] test, int addedColumnValue) {
//            Trace.Assert(columns.Length == test.Length);
            return columns.Count(c => c.HasAdditionalCoveredPair(test[c.Index], addedColumnValue));
         }

         public IEnumerable<Tuple<int, int, int>> EnumeratePairs() {
            var result = Enumerable.Empty<Tuple<int, int, int>>();
            foreach (var column in columns) {
               result = result.Concat(column.EnumeratePairs());
            }
            return result;
         }
      }

      public class PiColumn {
         private readonly int index;
         private readonly int combinatorialSetLength;
         private readonly int addedCombinatorialSetLength;
         private readonly bool[] grid;

         public PiColumn(int index, int combinatorialSetLength, int addedCombinatorialSetLength) {
            this.index = index;
            this.combinatorialSetLength = combinatorialSetLength;
            this.addedCombinatorialSetLength = addedCombinatorialSetLength;
            this.grid = new bool[combinatorialSetLength * addedCombinatorialSetLength];
         }

         public int Index => index;

         public void FlagCoveredPairs(int columnValue, int addedColumnValue) {
            grid[combinatorialSetLength * addedColumnValue + columnValue] = true;
         }

         public bool HasAdditionalCoveredPair(int columnValue, int addedColumnValue) {
            return !grid[combinatorialSetLength * addedColumnValue + columnValue];
         }

         public IEnumerable<Tuple<int, int, int>> EnumeratePairs() {
            for (var x = 0; x < combinatorialSetLength; x++) {
               for (var y = 0; y < addedCombinatorialSetLength; y++) {
                  var isFlagged = grid[combinatorialSetLength * y + x];
                  if (!isFlagged) {
                     yield return new Tuple<int, int, int>(index, x, y);
                  }
               }
            }
         }
      }
   }
}
