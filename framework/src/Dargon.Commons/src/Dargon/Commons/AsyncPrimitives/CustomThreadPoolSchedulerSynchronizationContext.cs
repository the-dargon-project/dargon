using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons.Pooling;
using Dargon.Commons.Scheduler;
using Dargon.Commons.Templating;

namespace Dargon.Commons.AsyncPrimitives {
   public class CustomThreadPoolSchedulerSynchronizationContext : SynchronizationContext {
      private readonly CustomThreadPoolScheduler scheduler;
      private IObjectPool<SendOrPostCallbackToActionAdapter> sendOrPostCallbackToActionAdapterPool;

      public CustomThreadPoolSchedulerSynchronizationContext(CustomThreadPoolScheduler scheduler) {
         this.scheduler = scheduler;
         this.sendOrPostCallbackToActionAdapterPool = ObjectPool.CreateTlsBacked<SendOrPostCallbackToActionAdapter>(CreateSendOrPostCallbackToActionAdapterContainer);
      }

      public bool IsRunningOnThreadPool => scheduler.IsCurrentThreadInPool;

      private SendOrPostCallbackToActionAdapter CreateSendOrPostCallbackToActionAdapterContainer(IObjectPool<SendOrPostCallbackToActionAdapter> pool) {
         var adapter = new SendOrPostCallbackToActionAdapter();
         adapter.CompletionSemapore = new Semaphore(0, 1);
         adapter.Action = state => {
            adapter.SendOrPostCallback(state);
            if (adapter.IsPostElseSend) {
               pool.ReturnObject(adapter);
            } else {
               adapter.CompletionSemapore.Release();
            }
         };
         return adapter;
      }

      public override void Send(SendOrPostCallback d, object? state) {
         if (IsRunningOnThreadPool) {
            d(state);
         } else {
            var adapter = sendOrPostCallbackToActionAdapterPool.TakeObject();
            adapter.IsPostElseSend = false;
            adapter.SendOrPostCallback = d;
            scheduler.Schedule(adapter.Action, state, null);
            adapter.CompletionSemapore.WaitOne();
            sendOrPostCallbackToActionAdapterPool.ReturnObject(adapter);
         }
      }

      [ThreadStatic] private static TlsState tlsState;

      public override void Post(SendOrPostCallback d, object? state) {
         var tlsStateCapture = tlsState ??= new TlsState();
         
         if (IsRunningOnThreadPool && tlsStateCapture.PostSynchronousExecutionOptimizationDepth < 50) {
            tlsStateCapture.PostSynchronousExecutionOptimizationDepth++;
            d(state);
            tlsStateCapture.PostSynchronousExecutionOptimizationDepth--;
         } else {
            var adapter = sendOrPostCallbackToActionAdapterPool.TakeObject();
            adapter.IsPostElseSend = true;
            adapter.SendOrPostCallback = d;
            scheduler.Schedule(adapter.Action, state, null);
         }
      }

      public override SynchronizationContext CreateCopy()
         => throw new NotImplementedException();

      private class SendOrPostCallbackToActionAdapter {
         public bool IsPostElseSend;
         public SendOrPostCallback SendOrPostCallback;
         public Semaphore CompletionSemapore;
         public Action<object> Action;
      }

      public class TlsState {
         public int PostSynchronousExecutionOptimizationDepth;
      }
   }
}
