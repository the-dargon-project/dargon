using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Vox.RoundTripTests;
using Xunit;

namespace Dargon.Vox.FunctionalTests.RoundTripTests {
   public class TupleRoundtripFT : RoundTripTest {
      [Fact] public void Test_ValueTuple_0() => MultiThreadedRoundTripTest(Enumerable.Range(0, 3).Select(i => (object)default(ValueTuple)).ToArray());
      [Fact] public void Test_ValueTuple_1() => MultiThreadedRoundTripTest(Enumerable.Range(0, 3).Select(i => CreatePlaceholder<ValueTuple<int>>()).ToArray());
      [Fact] public void Test_ValueTuple_2() => MultiThreadedRoundTripTest(Enumerable.Range(0, 3).Select(i => CreatePlaceholder<(int, string)>()).ToArray());
      [Fact] public void Test_ValueTuple_3() => MultiThreadedRoundTripTest(Enumerable.Range(0, 3).Select(i => CreatePlaceholder<(int, string, Guid)>()).ToArray());
      [Fact] public void Test_ValueTuple_4() => MultiThreadedRoundTripTest(Enumerable.Range(0, 3).Select(i => CreatePlaceholder<(int, string, Guid, bool)>()).ToArray());
      [Fact] public void Test_ValueTuple_5() => MultiThreadedRoundTripTest(Enumerable.Range(0, 3).Select(i => CreatePlaceholder<(int, string, Guid, bool, float)>()).ToArray());
      [Fact] public void Test_ValueTuple_6() => MultiThreadedRoundTripTest(Enumerable.Range(0, 3).Select(i => CreatePlaceholder<(int, string, Guid, bool, float, double)>()).ToArray());
      [Fact] public void Test_ValueTuple_7() => MultiThreadedRoundTripTest(Enumerable.Range(0, 3).Select(i => CreatePlaceholder<(int, string, Guid, bool, float, double, int)>()).ToArray());
      [Fact] public void Test_ValueTuple_8() => MultiThreadedRoundTripTest(Enumerable.Range(0, 3).Select(i => CreatePlaceholder<(int, string, Guid, bool, float, double, int, string)>()).ToArray());
      [Fact] public void Test_ValueTuple_Rest_2() => MultiThreadedRoundTripTest(Enumerable.Range(0, 3).Select(i => CreatePlaceholder<(int, string, Guid, bool, float, double, int, string, int, int, int, int)>()).ToArray());

      [Fact]
      public void Test_Simple() => MultiThreadedRoundTripTest(new[] {
         (0, 1, false, "Test0", Guid.Empty, new ConcurrentDictionary<int, string>()),
         (10, 20, true, "Test1", Guid.NewGuid(), new ConcurrentDictionary<int, string>()),
         (20, 3000000, false, "Test2", Guid.NewGuid(), new ConcurrentDictionary<int, string>())
      }, 1000, 8, AssertDeepEquals);

      [Fact]
      public void Test_ValueTuple_Rest() => MultiThreadedRoundTripTest(new[] {
         (0, 1, false, "Test0", Guid.Empty, new ConcurrentDictionary<int, string>(), 1, 2, 3, 4, 5, 6, 7),
         (10, 20, true, "Test1", Guid.NewGuid(), new ConcurrentDictionary<int, string>(), 1, 2, 3, 4, 5, 6, 7),
         (20, 3000000, false, "Test2", Guid.NewGuid(), new ConcurrentDictionary<int, string>(), 1, 2, 3, 4, 5, 6, 7)
      }, 1000, 8, AssertDeepEquals);
   }
}
