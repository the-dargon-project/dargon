using System.Threading;
using Dargon.Courier.AsyncPrimitives;
using Dargon.Ryu;
using NMockito;
using System.Threading.Tasks;
using Dargon.Courier.TestUtilities;
using Xunit;

namespace Dargon.Courier {
   public class MessagingTests : NMockitoInstance {
      private readonly IRyuContainer container;

      public MessagingTests() {
         var ryu = new RyuFactory().Create();
         container = new CourierContainerFactory(ryu).Create(new TestTransport());
      }

      [Fact(Timeout = 10000)]
      public async Task BroadcastTest() {
         var str = CreatePlaceholder<string>();

         var latch = new AsyncLatch();
         var router = container.GetOrThrow<InboundMessageRouter>();
         router.RegisterHandler<string>(async x => {
            await Task.Yield();

            AssertEquals(str, x.Body);
            latch.Set();
         });

         var messenger = container.GetOrThrow<Messenger>();
         await messenger.BroadcastAsync(str);

         await latch.WaitAsync();
      }

      [Fact(Timeout = 10000)]
      public async Task ReliableTest() {
         var str = CreatePlaceholder<string>();

         var latch = new AsyncLatch();
         var router = container.GetOrThrow<InboundMessageRouter>();
         router.RegisterHandler<string>(async x => {
            await Task.Yield();

            AssertEquals(str, x.Body);
            latch.Set();
         });

         var messenger = container.GetOrThrow<Messenger>();
         await messenger.BroadcastAsync(str);

         await latch.WaitAsync();
      }
   }
}
