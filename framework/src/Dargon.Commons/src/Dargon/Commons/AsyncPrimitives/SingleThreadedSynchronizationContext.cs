using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Dargon.Commons.AsyncPrimitives {
   public class SingleThreadedSynchronizationContext : SynchronizationContext {
      private readonly ConcurrentQueue<ContinuationContext> pendingContinuations = new();
      private readonly SemaphoreSlim pendingContinuationsSignal = new(0, int.MaxValue);
      private readonly Thread mainThread;

      public SingleThreadedSynchronizationContext() {
         mainThread = Thread.CurrentThread;
      }

      public bool IsCurrentThreadMainThread => Thread.CurrentThread == mainThread;

      public void WaitForAvailableWork() {
#if DEBUG
         IsCurrentThreadMainThread.AssertIsTrue();
#endif        
         pendingContinuationsSignal.Wait();
         pendingContinuationsSignal.Release();
      }

      public void ProcessTaskQueueTilEmpty() {
#if DEBUG
         IsCurrentThreadMainThread.AssertIsTrue();
#endif

         while (pendingContinuationsSignal.CurrentCount > 0) {
            pendingContinuationsSignal.Wait();
            var cc = pendingContinuations.DequeueOrThrow();
            ExecuteContinuationImmediatelyOnCurrentThread(
               cc.Callback,
               cc.State);
            cc.WaitEvent?.Set();
         }
      }

      public void ProcessTaskQueue() {
         while (true) {
            WaitForAvailableWork();
            ProcessTaskQueueTilEmpty();
         }
      }

      public override void Send(SendOrPostCallback cb, object? state) => InvokeImmediately(cb, state);

      public override void Post(SendOrPostCallback cb, object? state) => InvokeEventually(cb, state);

      private void InvokeImmediately(SendOrPostCallback cb, object? state) {
         if (IsCurrentThreadMainThread) {
            ExecuteContinuationImmediatelyOnCurrentThread(cb, state);
         } else {
            var continuationContext = AllocContinuationContext(cb, state, true);
            InvokeEventually(x => {
               var cc = (ContinuationContext)x;
               cc.Callback(cc.State);
               cc.WaitEvent.Set();
            }, continuationContext);
            continuationContext.WaitEvent.Reset();
         }
      }

      /// <summary>
      /// Counter to avoid stack-diving on Posts that are run synchronously.
      /// </summary>
      private int invokeEventuallyDepth = 0;

      private void InvokeEventually(SendOrPostCallback cb, object? state) {
         if (IsCurrentThreadMainThread && invokeEventuallyDepth < 50) {
            invokeEventuallyDepth++;
            ExecuteContinuationImmediatelyOnCurrentThread(cb, state);
            invokeEventuallyDepth--;
         } else {
            var continuationContext = AllocContinuationContext(cb, state, false);
            pendingContinuations.Enqueue(continuationContext);
            pendingContinuationsSignal.Release();
         }
      }

      private void ExecuteContinuationImmediatelyOnCurrentThread(SendOrPostCallback callback, object? state) {
         callback(state);
      }

      private ContinuationContext AllocContinuationContext(SendOrPostCallback callback, object? state, bool isForBlockingInvoke)
         => new() {
            SyncLock = new(),
            Callback = callback,
            State = state,
            WaitEvent = isForBlockingInvoke ? new AutoResetEvent(false) : null,
         };

      public override SynchronizationContext CreateCopy()
         => throw new NotImplementedException();

      private class ContinuationContext {
         public object SyncLock;
         public SendOrPostCallback Callback;
         public object State;
         public AutoResetEvent WaitEvent;
         public bool IsInvokeImmediatelyContext => WaitEvent != null;
      }
   }
}