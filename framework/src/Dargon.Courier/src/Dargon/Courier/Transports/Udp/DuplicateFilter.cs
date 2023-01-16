using Dargon.Commons.Collections;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Dargon.Commons;
using Dargon.Commons.AsyncPrimitives;
using Dargon.Courier.Utilities;
using SCG = System.Collections.Generic;

namespace Dargon.Courier.TransportTier.Udp {
   public class DuplicateFilter {
      private const int kFilterCount = 100;
      private const int kCycleFilterIntervalMillis = 1000;
      private const int kExpectedMessagesPerInterval = 40000;
      private const double kAcceptableFalsePositiveProbability = 1E-12;

      private readonly BloomFilter[] bloomFilters = Arrays.Create(kFilterCount, i => new BloomFilter(kExpectedMessagesPerInterval, kAcceptableFalsePositiveProbability));
      private readonly ConcurrentQueue<Tuple<Guid, AsyncBox<bool>>> queuedTestOperations = new ConcurrentQueue<Tuple<Guid, AsyncBox<bool>>>();
      private readonly Semaphore queuedTestOperationSignal = new Semaphore(0, int.MaxValue);
      private readonly object synchronization = new object();
   }
}
