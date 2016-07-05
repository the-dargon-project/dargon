using Dargon.Commons;
using Dargon.Commons.AsyncPrimitives;
using Dargon.Commons.Collections;
using Dargon.Courier.Utilities;
using System;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons.Exceptions;

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

      private ConcurrentSet<Guid> cs = new ConcurrentSet<Guid>();

      public Task<bool> IsNewAsync(Guid id) {
         return Task.FromResult(cs.TryAdd(id));
//         var box = new AsyncBox<bool>();
//         queuedTestOperations.Enqueue(Tuple.Create(id, box));
//         queuedTestOperationSignal.Release();
//         return box.GetResultAsync();
      }
   }
}
