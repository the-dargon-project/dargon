using System;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Courier.Utilities;

namespace Dargon.Courier.TransportTier.Udp {
   public class DuplicateFilter {
      private const int kFilterCount = 10;
      private const int kFilterIntervalMillis = 100;
      private const int kExpectedMessagesPerInterval = 10000;
      private const double kAcceptableFalsePositiveProbability = 1E-9;
      private readonly BloomFilter[] bloomFilters = Util.Generate(kFilterCount, i => new BloomFilter(kExpectedMessagesPerInterval, kAcceptableFalsePositiveProbability));
      private int epoch = 0;

      public void Initialize() {
         CycleFiltersAsync().Forget();
      }

      private async Task CycleFiltersAsync() {
         while (true) {
            await Task.Delay(kFilterIntervalMillis);
            var nextFilter = bloomFilters[(epoch + 1) % bloomFilters.Length];
            nextFilter.Clear();
            Interlocked.Increment(ref epoch);
         }
      }

      public async Task<bool> IsNewAsync(Guid id) {
         foreach (var filter in bloomFilters) {
            if (await filter.TestAsync(id)) {
               Console.WriteLine("Passed A");
               return false;
            }
         }
         var currentFilter = bloomFilters[epoch % bloomFilters.Length];
         Console.WriteLine("Passed To B");
         return await currentFilter.SetAndTestAsync(id);
      }
   }
}
