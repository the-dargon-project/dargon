using System;
using Dargon.Commons.Templating;
using Xunit;

namespace Dargon.Commons.Collections.Sorting {
   public class IntrospectiveSortTests {
      [Fact]
      public void IntrospectiveSortTest() {
         for (var it = 0; it < 100; it++) {
            var r = new Random(it);
            var values = Arrays.Create(r.Next(4 * IntrospectiveSort.IntrosortSizeThreshold), r.NextInt64());
            var indices = values.Map((x, i) => i);
            indices.IndirectSort(values, new Int64Comparer());

            for (var i = 1; i < indices.Length; i++) {
               values[indices[i - 1]].AssertIsLessThanOrEqualTo(values[indices[i]]);
            }
         }
      }

      [Fact]
      public void IntrospectiveSort2_IndirectSortTest() {
         for (var it = 0; it < 100; it++) {
            var r = new Random(it);
            var values = Arrays.Create(r.Next(4 * IntrospectiveSort.IntrosortSizeThreshold), r.NextInt64());
            var indices = values.Map((x, i) => i);
            indices.IndirectSort(values, new Int64FastComparer());

            for (var i = 1; i < indices.Length; i++) {
               values[indices[i - 1]].AssertIsLessThanOrEqualTo(values[indices[i]]);
            }
         }
      }

      [Fact]
      public void IntrospectiveSort2_SortTest() {
         for (var it = 0; it < 100; it++) {
            var r = new Random(it);
            var values = Arrays.Create(r.Next(4 * IntrospectiveSort.IntrosortSizeThreshold), r.NextInt64());
            values.Sort(new Int64FastComparer());

            for (var i = 1; i < values.Length; i++) {
               values[i - 1].AssertIsLessThanOrEqualTo(values[i]);
            }
         }
      }
   }
}
