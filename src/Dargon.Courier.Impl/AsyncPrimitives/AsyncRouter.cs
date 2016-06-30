using Dargon.Commons.Collections;
using Dargon.Commons.Pooling;
using Dargon.Courier.Vox;
using Dargon.Vox.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Dargon.Courier.AsyncPrimitives {
   public class AsyncRouter<TInput, TPassed> {
      private readonly ConcurrentTypeToDispatcherDictionary<TPassed> typeHandlers = new ConcurrentTypeToDispatcherDictionary<TPassed>();
      private readonly Func<TInput, Type> typeProjector;
      private readonly Func<TInput, TPassed> passedProjector;

      public AsyncRouter(Func<TInput, Type> typeProjector, Func<TInput, TPassed> passedProjector) {
         this.typeProjector = typeProjector;
         this.passedProjector = passedProjector;
      }

      public void RegisterHandler<T>(Func<TPassed, Task> handler) {
         var set = typeHandlers.GetOrAdd(typeof(T), add => new ConcurrentSet<Func<TPassed, Task>>());
         set.TryAdd(handler);
      }

      public async Task<bool> TryRouteAsync(TInput x) {
         var typeProjection = typeProjector(x);
         var passedProjection = passedProjector(x);
         ConcurrentSet<Func<TPassed, Task>> handlers;
         if (typeHandlers.TryGetValue(typeProjection, out handlers)) {
            await Task.WhenAll(handlers.Select(h => h(passedProjection)));
            return true;
         }
         return false;
      }

      public class ConcurrentTypeToDispatcherDictionary<TPassed> : CopyOnAddDictionary<Type, ConcurrentSet<Func<TPassed, Task>>> {

      }
   }
}
