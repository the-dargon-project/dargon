using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace NMockito2.Utilities {
   public class PairwiseCombinatorialSetGeneratorTests : NMockitoInstance {
      private readonly PairwiseCombinatorialSetGenerator testObj = new PairwiseCombinatorialSetGenerator();

      [Fact]
      public void GenerateSingleColumnSet_Test() {
         var results = testObj.GenerateCombinations(5);
         AssertSequenceEquals(new[] { 0, 1, 2, 3, 4 }, results.Select(x => x[0]));
         AssertEquals(5, results.Count);
      }

      [Fact]
      public void GenerateDoubleColumnSet_Test() {
         var combinatorialSetLengths = new[] { 5, 7 };
         var results = testObj.GenerateCombinations(combinatorialSetLengths);
         ValidatePairwiseCombinations(results, combinatorialSetLengths);
         AssertEquals(35, results.Count);
      }

      [Fact]
      public void GenerateTripleColumnSet_Test() {
         var combinatorialSetLengths = new[] { 2, 2, 8 };
         var results = testObj.GenerateCombinations(combinatorialSetLengths);
         ValidatePairwiseCombinations(results, combinatorialSetLengths);
         AssertEquals(16, results.Count);
      }

      [Fact]
      public void GenerateQuintupleColumnSet_Test() {
         var combinatorialSetLengths = new[] { 3, 5, 8, 5, 7 };
         var results = testObj.GenerateCombinations(combinatorialSetLengths);
         ValidatePairwiseCombinations(results, combinatorialSetLengths);
         AssertEquals(60, results.Count);
      }

      [Fact]
      public void GenerateOctupleColumnSet_Test() {
         var combinatorialSetLengths = new[] { 5, 4, 6, 7, 4, 9, 3, 7 };
         var results = testObj.GenerateCombinations(combinatorialSetLengths);
         ValidatePairwiseCombinations(results, combinatorialSetLengths);
         AssertEquals(77, results.Count);
      }

      private void ValidatePairwiseCombinations(IReadOnlyList<int[]> tests, params int[] combinatorialSetLengths) {
         for (var i = 0; i < tests.Count; i++) {
            Debug.Write($"{i}:\t");
            foreach (var value in tests[i]) {
               Debug.Write($"{value}\t");
            }
            Debug.WriteLine("");
         }

         var combinatorialSetLengthsLengthMinusOne = combinatorialSetLengths.Length - 1;
         for (var leftColumn = 0; leftColumn < combinatorialSetLengthsLengthMinusOne; leftColumn++) {
            for (var rightColumn = leftColumn + 1; rightColumn < combinatorialSetLengths.Length; rightColumn++) {
               for (var i = 0; i < combinatorialSetLengths[leftColumn]; i++) {
                  for (var j = 0; j < combinatorialSetLengths[rightColumn]; j++) {
                     var success = tests.Any(test => test[leftColumn] == i && test[rightColumn] == j);
                     if (!success) {
                        throw new Exception($"Could not find test for [{leftColumn}]={i} && [{rightColumn}]={j}!");
                     }
                  }
               }
            }
         }
      }
   }
}