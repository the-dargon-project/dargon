using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dargon.Commons.AsyncAwait {
   public static class SynchronizationContextExtensions {
      public static T AssertIsActivated<T>(this T x) where T : SynchronizationContext {
         Assert.ReferenceEquals(SynchronizationContext.Current, x);
         return x;
      }

      public static void Activate(this SynchronizationContext synchronizationContext) {
         SynchronizationContext.SetSynchronizationContext(synchronizationContext);
      }

      public static SwitchToSynchronizationContextAwaitable YieldToAsync(this SynchronizationContext synchronizationContext) 
         => new(synchronizationContext);

      public static PushSynchronizationContext ActivateTemporarily(this SynchronizationContext synchronizationContext)
         => new(synchronizationContext);

      public struct SwitchToSynchronizationContextAwaitable : IAwaitable<SynchronizationContextAwaiter> {
         private readonly SynchronizationContext sc;

         public SwitchToSynchronizationContextAwaitable(SynchronizationContext sc) {
            this.sc = sc;
         }

         public SynchronizationContextAwaiter GetAwaiter() => new(sc);
      }

      public readonly struct SynchronizationContextAwaiter : IAwaiterVoid {
         private readonly SynchronizationContext synchronizationContext;

         public SynchronizationContextAwaiter(SynchronizationContext synchronizationContext) {
            this.synchronizationContext = synchronizationContext;
         }

         // Returns whether the awaiting method can skip queuing a continuation and immediately execute.
         // This is only OK (true) if we're already running on the desired sync context.
         public bool IsCompleted => synchronizationContext == SynchronizationContext.Current;

         // Queues a continuation. This case only gets hit if IsCompleted above were false.
         public void OnCompleted(Action continuation) {
            synchronizationContext.Post(
               static (state) => ((Action)state)!(),
               continuation);
         }

         public void GetResult() { }
      }
   }

   public struct PushSynchronizationContext : IDisposable {
      private readonly bool shouldRestoreSynchronizationContext;
      private readonly SynchronizationContext originalSynchronizationContext;
      private bool isDisposed;

      public PushSynchronizationContext(SynchronizationContext sc) {
         var cur = SynchronizationContext.Current;
         shouldRestoreSynchronizationContext = cur != sc;
         originalSynchronizationContext = cur;

         SynchronizationContext.SetSynchronizationContext(sc);
         isDisposed = false;
      }

      public void Dispose() {
         if (isDisposed) return;
         isDisposed = true;

         if (shouldRestoreSynchronizationContext) {
            SynchronizationContext.SetSynchronizationContext(originalSynchronizationContext);
         }
      }
   }
}
