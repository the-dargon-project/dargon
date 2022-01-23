using NMockito;
using NMockito.Fluent;
using System.Threading.Tasks;
using Dargon.Commons.AsyncPrimitives;
using Xunit;

namespace Dargon.Courier {
   public class AsyncEventBusTests : NMockitoInstance {
      [Fact]
      public async Task Run() {
         var bus = new AsyncBus<int>();
         int val = 0;
         // whatt is this test? it seems wrong
         await bus.SubscribeAsync(async (producer, value) => {
            await Task.Delay(1000);
            val = value;
         });
         await bus.PostAsync(10);
         val.IsEqualTo(10);
      }
   }
}
