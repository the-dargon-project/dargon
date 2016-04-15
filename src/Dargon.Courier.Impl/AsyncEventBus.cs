using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dargon.Commons;

namespace Dargon.Courier {
   public class AsyncEventBus<T> : IAsyncEventBus<T> {
      public delegate Task Subscription(IAsyncProducer<T> self, T thing);

      private readonly LinkedList<Subscription> subscriptions = new LinkedList<Subscription>(); 

      public Task PostAsync(T thing) {
         var tasks = subscriptions.Select(s => DargonCommonsExtensions.Forgettable(s(this, thing)));
         return Task.WhenAll(tasks);
      }

      public void Subscribe(Func<IAsyncProducer<T>, T, Task> handler) {
         subscriptions.AddFirst(new Subscription(handler));
      }
   }
}