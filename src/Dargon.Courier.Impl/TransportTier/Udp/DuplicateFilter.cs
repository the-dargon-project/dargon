using System;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Courier.Utilities;
using Nito.AsyncEx;

namespace Dargon.Courier.TransportTier.Udp {
   public class DuplicateFilter {
      private const int kFilterCount = 10;
      private const int kFilterIntervalMillis = 100;
      private const int kExpectedMessagesPerInterval = 10000;
      private const double kAcceptableFalsePositiveProbability = 1E-9;
      private readonly BloomFilter[] bloomFilters = Util.Generate(kFilterCount, i => new BloomFilter(kExpectedMessagesPerInterval, kAcceptableFalsePositiveProbability));
      private readonly AsyncReaderWriterLock synchronization = new AsyncReaderWriterLock();
      private int epoch = 0;

      public void Initialize() {
         CycleFiltersAsync().Forget();
      }

      private async Task CycleFiltersAsync() {
         while (true) {
            await Task.Delay(kFilterIntervalMillis).ConfigureAwait(false);
            using (await synchronization.WriterLockAsync().ConfigureAwait(false)) {
               var nextFilter = bloomFilters[(epoch + 1) % bloomFilters.Length];
               nextFilter.Clear();
               Interlocked.Increment(ref epoch);
            }
         }
      }

      public async Task<bool> IsNewAsync(Guid id) {
         using (await synchronization.ReaderLockAsync().ConfigureAwait(false)) {
            if (!await TestIsNewAsyncUnderLock(id).ConfigureAwait(false)) {
               return false;
            }
         }
         using (await synchronization.WriterLockAsync().ConfigureAwait(false)) {
            if (!await TestIsNewAsyncUnderLock(id).ConfigureAwait(false)) {
               return false;
            }

            var currentFilter = bloomFilters[epoch % bloomFilters.Length];
            return await currentFilter.SetAndTestAsync(id).ConfigureAwait(false);
         }
      }

      private async Task<bool> TestIsNewAsyncUnderLock(Guid id) {
         foreach (var filter in bloomFilters) {
            if (await filter.TestAsync(id).ConfigureAwait(false)) {
               return false;
            }
         }
         return true;
      }
   }
}
