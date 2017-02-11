﻿using System.Threading;
using System.Threading.Tasks;

namespace Dargon.Commons.AsyncPrimitives {
   public class AsyncBox<T> {
      private readonly TaskCompletionSource<T> tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

      public void SetResult(T value) {
         tcs.SetResult(value);
      }

      public Task<T> GetResultAsync(CancellationToken cancellationToken = default(CancellationToken)) {
         return tcs.Task;
      }
   }
}
