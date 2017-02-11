using Dargon.Commons.Collections;
using System;
using System.Linq;
using SCG = System.Collections.Generic;

namespace Dargon.Courier.TransportTier.Udp {
   public class DuplicateFilter {
      private const int kFilterCount = 100;
      private const int kCycleFilterIntervalMillis = 1000;
      private const int kExpectedMessagesPerInterval = 40000;
      private const double kAcceptableFalsePositiveProbability = 1E-12;

//      private readonly BloomFilter[] bloomFilters = Util.Generate(kFilterCount, i => new BloomFilter(kExpectedMessagesPerInterval, kAcceptableFalsePositiveProbability));
//      private readonly ConcurrentQueue<Tuple<Guid, AsyncBox<bool>>> queuedTestOperations = new ConcurrentQueue<Tuple<Guid, AsyncBox<bool>>>();
//      private readonly Semaphore queuedTestOperationSignal = new Semaphore(0, int.MaxValue);
//      private readonly object synchronization = new object();
//      private int epoch = 0;

      public void Initialize() {
//         new Thread(() => {
//            while (true) {
//               queuedTestOperationSignal.WaitOne();
//               lock (synchronization) {
//                  var epochCapture = Interlocked.CompareExchange(ref epoch, 0, 0);
//                  var bloomFilter = bloomFilters[epochCapture % bloomFilters.Length];
//                  Tuple<Guid, AsyncBox<bool>> op;
//                  if (!queuedTestOperations.TryDequeue(out op)) {
//                     throw new InvalidStateException();
//                  }
//                  var result = bloomFilter.SetAndTest(op.Item1);
//                  op.Item2.SetResult(result);
//               }
//            }
//         }) { IsBackground = true }.Start();
//
//         new Thread(() => {
//            while (true) {
//               Thread.Sleep(kCycleFilterIntervalMillis);
//               lock (synchronization) {
//                  var epochCapture = Interlocked.CompareExchange(ref epoch, 0, 0);
//                  var nextFilter = bloomFilters[(epochCapture + 1) % bloomFilters.Length];
//                  nextFilter.Clear();
//                  Interlocked.Increment(ref epoch);
//               }
//            }
//         }) { IsBackground = true }.Start();
      }

//      private readonly object synchronization = new object();
//      private HashSet<Guid> seenIds = new HashSet<Guid>();
      private ConcurrentSet<Guid> seenIds = new ConcurrentSet<Guid>(1024, 30000);

      public SCG.IReadOnlyDictionary<Guid, bool> TestPacketIdsAreNew(IReadOnlySet<Guid> queryIds) {
         return queryIds.ToDictionary(
            q => q,
            seenIds.TryAdd);

//         var results = new SCG.Dictionary<Guid, bool>();
//
//         HashSet<Guid> newIdCandidates = new HashSet<Guid>();
//         foreach (var queryId in queryIds) {
//            if (seenIds.Contains(queryId)) {
//               results[queryId] = false;
//            } else {
//               newIdCandidates.Add(queryId);
//            }
//         }
//
//         lock (synchronization) {
//            var nextSeenIds = new HashSet<Guid>(seenIds);
//            foreach (var queryId in newIdCandidates) {
//               results[queryId] = nextSeenIds.Add(queryId);
//            }
//            seenIds = nextSeenIds;
//         }
//
//         return results;
      }
   }
}
