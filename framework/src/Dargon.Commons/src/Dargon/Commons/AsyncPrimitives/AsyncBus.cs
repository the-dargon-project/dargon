using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons.Utilities;

namespace Dargon.Commons.AsyncPrimitives {
   public class AsyncBus<T> : IAsyncBus<T> {
      private readonly AsyncReaderWriterLock sync = new();
      private readonly List<Subscription> subscriptions = new(); 

      public async Task PostAsync(T thing) {
         Subscription[] subsCapture;
         {
            await using var mut = await sync.CreateReaderGuardAsync();
            subsCapture = subscriptions.ToArray();
         }

         var tasks = new Task[subsCapture.Length];
         for (var i = 0; i < subsCapture.Length; i++) {
            tasks[i] = subsCapture[i].Notify(thing);
         }

         await Task.WhenAll(tasks);
      }

      public async Task<IAsyncDisposable> SubscribeAsync(SubscriberCallbackFunc<T> callbackFunc) {
         var subscription = new Subscription(this, callbackFunc);

         await using var mut = await sync.CreateWriterGuardAsync();
         subscriptions.Add(subscription);
         return subscription;
      }


      private async Task HandleSubscriptionDisposedAsync(Subscription subscription) {
         await using var mut = await sync.CreateWriterGuardAsync();
         subscriptions.Remove(subscription).AssertIsTrue();
      }

      private class Subscription : IAsyncDisposable {
         private readonly AsyncBus<T> bus;
         private readonly AsyncReaderWriterLock arwl = new();
         private SubscriberCallbackFunc<T> callbackFunc;

         public Subscription(AsyncBus<T> bus, SubscriberCallbackFunc<T> callbackFunc) {
            this.bus = bus;
            this.callbackFunc = callbackFunc;
         }

         public async Task Notify(T thing) {
            // the lock mutually excludes dispose from notifies so that once
            // dispose acquires a writer lock, no further notifies may happen.
            await using var _ = await arwl.CreateReaderGuardAsync();
            // It is important this read happens within the reader guard;
            // if it were outside and above, then we could enter the reader
            // after the writer runs & clears the callback, but hold onto callback.
            var cb = Interlocked2.Read(ref callbackFunc);
            if (cb != null) {
               await cb.Invoke(bus, thing);
            }
         }

         public async ValueTask DisposeAsync() {
            {
               // set callbackFunc to null to begin stopping callback invokes.
               Interlocked2.WriteOrThrow(ref callbackFunc, null);

               // acquire an exclusive lock, indicating no callback invocations
               // are happening concurrently. We can make two inferences:
               // 1. Nobody is in the reader lock anymore, since we have a writer lock.
               // 2. As callbackFunc is null, once we leave this writer lock,
               //    no further reader-locker will invoke the callback.
               await using var _ = await arwl.CreateWriterGuardAsync();
            }

            // as noted above, the callback can no-longer be notified.
            // removal happens asynchronously. what matters is cb can't be invoked
            bus.HandleSubscriptionDisposedAsync(this).Forget();
         }
      }
   }
}