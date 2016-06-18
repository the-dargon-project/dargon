﻿using Dargon.Commons;
using NMockito;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using static Dargon.Vox.VoxStatics;

namespace Dargon.Vox.RoundTripTests {
   public class RoundTripTest : NMockitoInstance {
      private VoxSerializer serializer;

      protected RoundTripTest(Type[] testTypes) {
         serializer = new VoxFactory().Create();
         var typeIdCounter = 0;
         var typeContextsByTypeId = testTypes.ToDictionary(
            testType => typeIdCounter++,
            testType => Something(testType));
         serializer.ImportTypes(new InlineVoxTypes(typeContextsByTypeId));
      }

      public void MultiThreadedRoundTripTest<T>(IReadOnlyList<T> testCases, int trialsPerCase, int workerCount) {
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
                  RunRoundTripTest(testCase, $"Worker {workerId}: RTT {testCase}", trialsPerCase);
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

      public void RunRoundTripTest<T>(T val, string benchmarkName = null, int benchmarkCount = -1) {
         if (benchmarkCount < 0) {
            new Runner(serializer).Run(val, 1);
         } else {
            var sw = new Stopwatch();
            sw.Start();
            new Runner(serializer).Run(val, benchmarkCount);
            Console.WriteLine($"`{benchmarkName}` benchmark completed {benchmarkCount} trials in {sw.ElapsedMilliseconds} ms");
         }
      }

      public class Runner : NMockitoInstance {
         private readonly VoxSerializer serializer;

         public Runner(VoxSerializer serializer) {
            this.serializer = serializer;
         }

         public void Run<T>(T val, int count) {
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
               T[] results = Util.Generate(count, i => (T)serializer.Deserialize(ms, hintType));
               AssertEquals(length, ms.Position);
               foreach (var val2 in results) {
                  var val2Type = val2?.GetType();
                  if (typeof(Type).IsAssignableFrom(val2Type)) {
                     val2Type = typeof(Type);
                  }
                  AssertEquals(hintType, val2Type);
                  if (val == null) {
                     AssertNull(val2);
                  } else if (val is IEnumerable) {
                     var e1 = ((IEnumerable)val).Cast<object>().ToArray();
                     var e2 = ((IEnumerable)val2).Cast<object>().ToArray();
                     AssertSequenceEquals(e1, e2);
                     e1.Zip(e2, Tuple.Create).ForEach(t => AssertEquals(t.Item1?.GetType(), t.Item2?.GetType()));
                  } else {
                     AssertEquals(val, val2);
                  }
               }
            }
         }
      }
   }
}
