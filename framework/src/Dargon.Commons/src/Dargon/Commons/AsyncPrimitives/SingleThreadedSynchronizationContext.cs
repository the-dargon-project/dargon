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

      /// <summary>
      /// This method must be reentrant; the game loop runs on this sync context and
      /// will drain the task queue per frame.
      /// </summary>
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

      public override void Send(SendOrPostCallback cb, object stateOpt) => InvokeImmediately(cb, stateOpt);

      public override void Post(SendOrPostCallback cb, object stateOpt) => InvokeEventually(cb, stateOpt);

      private void InvokeImmediately(SendOrPostCallback cb, object stateOpt) {
         if (IsCurrentThreadMainThread) {
            ExecuteContinuationImmediatelyOnCurrentThread(cb, stateOpt);
         } else {
            var continuationContext = AllocContinuationContext(cb, stateOpt, true);
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

      private void InvokeEventually(SendOrPostCallback cb, object stateOpt) {
         if (IsCurrentThreadMainThread && invokeEventuallyDepth < 50) {
            invokeEventuallyDepth++;
            ExecuteContinuationImmediatelyOnCurrentThread(cb, stateOpt);
            invokeEventuallyDepth--;
         } else {
            var continuationContext = AllocContinuationContext(cb, stateOpt, false);
            pendingContinuations.Enqueue(continuationContext);
            pendingContinuationsSignal.Release();
         }
      }

      private void ExecuteContinuationImmediatelyOnCurrentThread(SendOrPostCallback callback, object stateOpt) {
         callback(stateOpt);
      }

      private ContinuationContext AllocContinuationContext(SendOrPostCallback callback, object stateOpt, bool isForBlockingInvoke)
         => new() {
            SyncLock = new(),
            Callback = callback,
            State = stateOpt,
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