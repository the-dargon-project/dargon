using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Dargon.Commons.AsyncPrimitives {
   public class AsyncBus<T> : IAsyncBus<T> {
      private readonly AsyncReaderWriterLock sync = new();
      private readonly List<Subscription> subscriptions = new(); 

      public async Task PostAsync(T thing) {
         using var mut = await sync.ReaderLockAsync();
         var tasks = subscriptions.Map(s => s.Notify(thing));
         await Task.WhenAll(tasks);
      }

      public async Task<IDisposable> SubscribeAsync(SubscriberCallbackFunc<T> callbackFunc) {
         using var mut = await sync.WriterLockAsync();
         var subscription = new Subscription(this, callbackFunc);
         subscriptions.Add(subscription);
         return subscription;
      }


      private async Task HandleSubscriptionDisposedAsync(Subscription subscription) {
         using var mut = await sync.WriterLockAsync();
         subscriptions.Remove(subscription).AssertIsTrue();
      }

      private class Subscription : IDisposable {
         private readonly AsyncBus<T> bus;
         private SubscriberCallbackFunc<T> callbackFunc;
         private int isDisposed = 0;

         public Subscription(AsyncBus<T> bus, SubscriberCallbackFunc<T> callbackFunc) {
            this.bus = bus;
            this.callbackFunc = callbackFunc;
         }

         public void Dispose() {
            Interlocked.CompareExchange(ref isDisposed, 1, 0).AssertEquals(0);
            Interlocked.CompareExchange(ref callbackFunc, null, callbackFunc);
            bus.HandleSubscriptionDisposedAsync(this).Forget();
         }

         public Task Notify(T thing) {
            // cb can be null if we've disposed
            var cbf = Interlocked.CompareExchange(ref callbackFunc, null, null);
            return cbf?.Invoke(bus, thing);
         }
      }
   }
}