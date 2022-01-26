using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons.Pooling;

namespace Dargon.Commons.Scheduler {
   public interface IThreadInternal {
      void Start();
      int ManagedThreadId { get; }
   }

   public interface IThreadFactory {
      IThreadInternal Create(Action threadStart, string name = null);
   }

   public class ThreadFactory : IThreadFactory {
      public IThreadInternal Create(Action threadStart, string name = null) {
         var thread = new Thread(() => threadStart()) { Name = name, IsBackground = true };
         return new ThreadInternalBox { Thread = thread };
      }

      private class ThreadInternalBox : IThreadInternal {
         public Thread Thread;

         public void Start() {
            Thread.Start();
         }

         public int ManagedThreadId => Thread.ManagedThreadId;
      }
   }

   public class DefaultJobQueue<TJobData> : IJobQueue<TJobData> {
      private readonly IScheduler scheduler;
      private readonly Action<TJobData> jobHandler;
      private readonly IObjectPool<ExecutionContext> executionContextPool;

      public DefaultJobQueue(IScheduler scheduler, Action<TJobData> jobHandler) {
         this.scheduler = scheduler;
         this.jobHandler = jobHandler;
         this.executionContextPool = ObjectPool.CreateConcurrentQueueBacked<ExecutionContext>(() => {
            var ec = new ExecutionContext();
            ec.SchedulerHandler = _ => jobHandler(ec.UserData);
            ec.SchedulerState = null;
            ec.SchedulerCallback = () => {
               ec.UserCallback();
               
               ec.UserData = default;
               ec.UserCallback = default;
               executionContextPool.ReturnObject(ec);
            };
            return ec;
         });
      }

      public void Enqueue(TJobData data) {
         var ec = executionContextPool.TakeObject();
         ec.UserData = data;
         ec.UserCallback = null;

         scheduler.Schedule(ec.SchedulerHandler, ec.SchedulerState, ec.SchedulerCallback);
      }

      public void EnqueueWithCallback(TJobData data, Action callback) {
         var ec = executionContextPool.TakeObject();
         ec.UserData = data;
         ec.UserCallback = callback;

         scheduler.Schedule(ec.SchedulerHandler, ec.SchedulerState, ec.SchedulerCallback);
      }

      public Task EnqueueAndAwaitAsync(TJobData data) {
         var tcs = new TaskCompletionSource();
         EnqueueWithCallback(data, tcs.SetResult);
         return tcs.Task;
      }

      public class ExecutionContext {
         public Action<object> SchedulerHandler;
         public object SchedulerState;
         public Action SchedulerCallback;

         public TJobData UserData;
         public Action UserCallback;
      }
   }

   public class DefaultRequestResponseJobQueue<TJobRequest, TJobResponse> : IRequestResponseJobQueue<TJobRequest, TJobResponse> {
      private readonly IScheduler scheduler;
      private readonly Func<TJobRequest, TJobResponse> jobHandler;

      public DefaultRequestResponseJobQueue(IScheduler scheduler, Func<TJobRequest, TJobResponse> jobHandler) {
         this.scheduler = scheduler;
         this.jobHandler = jobHandler;
      }

      public void EnqueueWithCallback(TJobRequest request, Action<TJobResponse> callback) {
         scheduler.Schedule(_ => jobHandler(request), callback);
      }

      public Task<TJobResponse> EnqueueAndAwaitAsync(TJobRequest request) {
         return scheduler.ExecuteAsync(() => jobHandler(request));
      }
   }
}
