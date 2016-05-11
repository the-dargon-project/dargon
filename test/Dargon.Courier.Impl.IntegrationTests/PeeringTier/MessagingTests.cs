using System.Threading;
using Dargon.Courier.AsyncPrimitives;
using Dargon.Ryu;
using NMockito;
using System.Threading.Tasks;
using Dargon.Courier.TestUtilities;
using Xunit;

namespace Dargon.Courier {
   public class MessagingTests : NMockitoInstance {
      private readonly IRyuContainer senderContainer;
      private readonly IRyuContainer receiverContainer;

      public MessagingTests() {
         var transport = new TestTransport();
         senderContainer = new CourierContainerFactory(new RyuFactory().Create()).Create(transport);
         receiverContainer = new CourierContainerFactory(new RyuFactory().Create()).Create(transport);
      }

      [Fact(Timeout = 10000)]
      public async Task BroadcastTest() {
         var str = CreatePlaceholder<string>();

         var latch = new AsyncLatch();
         var router = receiverContainer.GetOrThrow<InboundMessageRouter>();
         router.RegisterHandler<string>(async x => {
            await Task.Yield();

            AssertEquals(str, x.Body);
            latch.Set();
         });

         await senderContainer.GetOrThrow<Messenger>().BroadcastAsync(str);
         await latch.WaitAsync();
      }

      [Fact(Timeout = 10000)]
      public async Task ReliableTest() {
         var str = CreatePlaceholder<string>();

         var latch = new AsyncLatch();
         var router = receiverContainer.GetOrThrow<InboundMessageRouter>();
         router.RegisterHandler<string>(async x => {
            await Task.Yield();

            AssertEquals(str, x.Body);
            latch.Set();
         });

         var messenger = senderContainer.GetOrThrow<Messenger>();
         await messenger.SendReliableAsync(str, receiverContainer.GetOrThrow<Identity>().Id);
         await latch.WaitAsync();
      }
   }
}
