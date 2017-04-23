using Dargon.Commons;
using NMockito;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace Dargon.Vox.RoundTripTests {
   public class RoundTripTest : NMockitoInstance {
      protected readonly VoxSerializer serializer;

      protected RoundTripTest(Type[] testTypes = null) {
         testTypes = testTypes ?? new Type[0];
         serializer = new VoxFactory().Create();
         var typeIdCounter = 1;
         var typeContextsByTypeId = testTypes.ToDictionary(
            testType => typeIdCounter++,
            TypeContext.Create);
         serializer.ImportTypes(new InlineVoxTypes(0, typeContextsByTypeId));
      }

      public void MultiThreadedRoundTripTest<T>(IReadOnlyList<T> testCases, int trialsPerCase, int workerCount, Action<T, T> assertEqualsOverride = null) {
         var workersReadySignal = new CountdownEvent(workerCount);
         var startSignal = new ManualResetEvent(false);
         var workersCompleteSignal = new CountdownEvent(workerCount);

         var dts = new double[workerCount];

         for (var i = 0; i < workerCount; i++) {
            var workerId = i;
            new Thread(() => {
               workersReadySignal.Signal();
               startSignal.WaitOne();
               var sw = new Stopwatch();
               sw.Start();
               foreach (var testCase in testCases.Shuffle(new Random(workerId))) {
                  RunRoundTripTest(testCase, $"Worker {workerId}: RTT {testCase}", trialsPerCase, assertEqualsOverride);
               }
               dts[workerId] = sw.ElapsedMilliseconds;
               workersCompleteSignal.Signal();
            }).Start();
         }
         workersReadySignal.Wait();
         startSignal.Set();
         workersCompleteSignal.Wait();

         Console.WriteLine("Done in: " + dts.Join(", ") + " (avg " + (dts.Sum() / dts.Length) + " )");
      }

      public void RunRoundTripTest<T>(T val, string benchmarkName = null, int benchmarkCount = -1, Action<T, T> assertEqualsOverride = null) {
         if (benchmarkCount < 0) {
            new Runner<T>(serializer, assertEqualsOverride).Run(val, 1);
         } else {
            var sw = new Stopwatch();
            sw.Start();
            new Runner<T>(serializer, assertEqualsOverride).Run(val, benchmarkCount);
            Console.WriteLine($"`{benchmarkName}` benchmark completed {benchmarkCount} trials in {sw.ElapsedMilliseconds} ms");
         }
      }

      public class Runner<T> : NMockitoInstance {
         private readonly VoxSerializer serializer;
         private readonly Action<T, T> assertEqualsOverride;

         public Runner(VoxSerializer serializer, Action<T, T> assertEqualsOverride) {
            this.serializer = serializer;
            this.assertEqualsOverride = assertEqualsOverride;
         }

         public void Run(T val, int count) {
            using (var ms = new MemoryStream()) {
               for (var i = 0; i < count; i++) {
                  serializer.Serialize(ms, val);
               }
               var length = ms.Position;
               ms.Position = 0;
               var hintType = val?.GetType();
               if (typeof(Type).IsAssignableFrom(hintType)) {
                  hintType = typeof(Type);
               }
               T[] results = Arrays.Create(count, i => (T)serializer.Deserialize(ms, hintType));
               AssertEquals(length, ms.Position);
               foreach (var val2 in results) {
                  if (assertEqualsOverride != null) {
                     assertEqualsOverride(val, val2);
                  } else {
                     DefaultAssertEquals(val, val2, hintType);
                  }
               }
            }
         }

         public void DefaultAssertEquals(T expected, T actual, Type hintType) {
            if (assertEqualsOverride != null) {
               assertEqualsOverride(expected, actual);
               return;
            }

            var val2Type = actual?.GetType();
            if (typeof(Type).IsAssignableFrom(val2Type)) {
               val2Type = typeof(Type);
            }
            AssertEquals(hintType, val2Type);
            if (expected == null) {
               AssertNull(actual);
            } else if (expected is IEnumerable) {
               var e1 = ((IEnumerable)expected).Cast<object>().ToArray();
               var e2 = ((IEnumerable)actual).Cast<object>().ToArray();
               AssertCollectionDeepEquals(e1, e2);
            } else {
               AssertEquals(expected, actual);
            }
         }
      }
   }
}
