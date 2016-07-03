using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Commons.Collections;
using Dargon.Courier.AsyncPrimitives;
using Dargon.Courier.Utilities;
using Nito.AsyncEx;

namespace Dargon.Courier.TransportTier.Udp {
   public class DuplicateFilter {
      private const int kFilterCount = 10;
      private const int kCycleFilterIntervalMillis = 100;
      private const int kExpectedMessagesPerInterval = 40000;
      private const double kAcceptableFalsePositiveProbability = 1E-12;

      private readonly BloomFilter[] bloomFilters = Util.Generate(kFilterCount, i => new BloomFilter(kExpectedMessagesPerInterval, kAcceptableFalsePositiveProbability));
      private readonly ConcurrentQueue<Tuple<Guid, AsyncBox<bool>>> queuedTestOperations = new ConcurrentQueue<Tuple<Guid, AsyncBox<bool>>>();
      private readonly AutoResetEvent queuedTestOperationSignal = new AutoResetEvent(false);
      private readonly object synchronization = new object();
      private int epoch = 0;

      public void Initialize() {
         new Thread(() => {
            while (true) {
               queuedTestOperationSignal.WaitOne();
               lock (synchronization) {
                  var bloomFilter = bloomFilters[epoch % bloomFilters.Length];
                  foreach (var op in queuedTestOperations) {
                     var result = bloomFilter.SetAndTest(op.Item1);
                     op.Item2.SetResult(result);
                  }
               }
            }
         }) { IsBackground = true }.Start();

         new Thread(() => {
            while (true) {
               Thread.Sleep(kCycleFilterIntervalMillis);
               lock (synchronization) {
                  var nextFilter = bloomFilters[(epoch + 1) % bloomFilters.Length];
                  nextFilter.Clear();
                  Interlocked.Increment(ref epoch);
               }
            }
         }) { IsBackground = true }.Start();
      }

      private readonly ConcurrentSet<Guid> s = new ConcurrentSet<Guid>();

      public Task<bool> IsNewAsync(Guid id) {
         return Task.FromResult(s.TryAdd(id));
         var box = new AsyncBox<bool>();
         queuedTestOperations.Enqueue(Tuple.Create(id, box));
         queuedTestOperationSignal.Set();
         return box.GetResultAsync();
      }
   }
}
