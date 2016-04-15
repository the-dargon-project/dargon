using Dargon.Commons;
using Dargon.Courier.Utilities;
using Dargon.Courier.Vox;
using System.Threading;
using System.Threading.Tasks;

namespace Dargon.Courier {
   public class ReliableMessageFilter {
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

      public bool IsNewMessage(MessageDto message) {
         var filter = bloomFilters[((uint)epoch) % bloomFilters.Length];
         return filter.SetAndTest(message.Id);
      }
   }
}
