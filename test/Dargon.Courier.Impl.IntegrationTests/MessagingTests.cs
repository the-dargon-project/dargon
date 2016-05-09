using System.Threading;
using Dargon.Courier.AsyncPrimitives;
using Dargon.Ryu;
using NMockito;
using System.Threading.Tasks;
using Dargon.Courier.TestUtilities;
using Xunit;

namespace Dargon.Courier {
   public class MessagingTests : NMockitoInstance {
      private readonly IRyuContainer app1Container;
      private readonly IRyuContainer app2Container;

      public MessagingTests() {
         var transport = new TestTransport();
         app1Container = new CourierContainerFactory(new RyuFactory().Create()).Create(transport);
         app2Container = new CourierContainerFactory(new RyuFactory().Create()).Create(transport);
      }

      [Fact(Timeout = 10000)]
      public async Task BroadcastTest() {
         var str = CreatePlaceholder<string>();

         var latch = new AsyncLatch();
         var router = app1Container.GetOrThrow<InboundMessageRouter>();
         router.RegisterHandler<string>(async x => {
            await Task.Yield();

            AssertEquals(str, x.Body);
            latch.Set();
         });

         var messenger = app1Container.GetOrThrow<Messenger>();
         await messenger.BroadcastAsync(str);

         await latch.WaitAsync();
      }

      [Fact(Timeout = 10000)]
      public async Task ReliableTest() {
         var str = CreatePlaceholder<string>();

         var latch = new AsyncLatch();
         var router = app1Container.GetOrThrow<InboundMessageRouter>();
         router.RegisterHandler<string>(async x => {
            await Task.Yield();

            AssertEquals(str, x.Body);
            latch.Set();
         });

         var messenger = app1Container.GetOrThrow<Messenger>();
         await messenger.BroadcastAsync(str);

         await latch.WaitAsync();
      }

      [Fact(Timeout = 10000)]
      public async Task ReliableTest() {
         var str = CreatePlaceholder<string>();

         var latch = new AsyncLatch();
         var router = app1Container.GetOrThrow<InboundMessageRouter>();
         router.RegisterHandler<string>(async x => {
            await Task.Yield();

            AssertEquals(str, x.Body);
            latch.Set();
         });

         var messenger = app1Container.GetOrThrow<Messenger>();
         await messenger.BroadcastAsync(str);

         await latch.WaitAsync();
      }
   }
}
