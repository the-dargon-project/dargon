using System;
using System.Diagnostics;
using System.Linq;
using Dargon.Commons;
using NMockito;
using Xunit;

namespace Dargon.Courier.Utilities {
   public class BloomFilterTests : NMockitoInstance {
      private const int kExpectedItems = 10000;
      private const double kExpectedCollisionProbability = 1E-9;

      private readonly BloomFilter testObj = new BloomFilter(kExpectedItems, kExpectedCollisionProbability);
      
      [Fact]
      public void PerformanceTest() {
         const int guidCount = 200000;
         const int timeAllowed = 1000;
         var guids = Util.Generate(guidCount, Guid.NewGuid);
         var sw = new Stopwatch();
         sw.Start();
         guids.ForEach(bit => testObj.SetAndTest(bit));
         sw.Stop();
         if (sw.ElapsedMilliseconds > timeAllowed) {
            throw new Exception($"SetAndTest on {guidCount} guids took {sw.ElapsedMilliseconds} > {timeAllowed} millis");
         }
      }

      [Fact]
      public void CanHaveNegligibleFalseCollisionRate() {
         const int guidCount = kExpectedItems;

         var guids = Util.Generate(guidCount, Guid.NewGuid);
         AssertEquals(guids.Distinct().Count(), guidCount);

         foreach (var guid in guids) {
            AssertTrue(testObj.SetAndTest(guid));
         }

         foreach (var guid in guids) {
            AssertFalse(testObj.SetAndTest(guid));
         }
      }
   }
}
