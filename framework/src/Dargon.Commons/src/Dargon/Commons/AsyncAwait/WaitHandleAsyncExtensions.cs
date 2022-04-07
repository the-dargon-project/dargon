using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dargon.Commons.AsyncAwait {
   public static class WaitHandleAsyncExtensions {
      public static async Task<bool> WaitOneAsync(this WaitHandle waitHandle, int timeoutMillis, CancellationToken ctok = default) {
         RegisteredWaitHandle registeredWaitHandle = null;
         CancellationTokenRegistration tokenRegistration = default(CancellationTokenRegistration);
         bool isCancellable = ctok.CanBeCanceled;
         try {
            var tcs = new TaskCompletionSource<bool>();
            const bool kExecuteOnlyOnce = true;
            registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject(
               waitHandle,
               (state, timedOut) => ((TaskCompletionSource<bool>)state)!.TrySetResult(!timedOut),
               tcs,
               timeoutMillis,
               kExecuteOnlyOnce);
            
            if (isCancellable) {
               tokenRegistration = ctok.Register(
                  state => ((TaskCompletionSource<bool>)state).TrySetCanceled(),
                  tcs);
            }

            return await tcs.Task;
         } finally {
            registeredWaitHandle?.Unregister(null);

            if (isCancellable) {
               await tokenRegistration.DisposeAsync();
            }
         }
      }

      public static Task<bool> WaitOneAsync(this WaitHandle waitHandle, TimeSpan timeout, CancellationToken ctok = default) {
         return waitHandle.WaitOneAsync((int)timeout.TotalMilliseconds, ctok);
      }

      public static Task<bool> WaitOneAsync(this WaitHandle waitHandle, CancellationToken ctok = default) {
         return waitHandle.WaitOneAsync(-1, ctok);
      }
   }
}
