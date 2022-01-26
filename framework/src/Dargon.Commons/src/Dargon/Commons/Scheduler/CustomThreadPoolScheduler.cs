using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons.AsyncPrimitives;
using Dargon.Commons.Pooling;

namespace Dargon.Commons.Scheduler {
   public class CustomThreadPoolScheduler : IScheduler {
      private class Work {
         public Action<object> WorkFunc;
         public object State;
         public Action CompletionCallback;
         public Action ReturnToPool;
      }

      private readonly ConcurrentQueue<(Action<object>, object)> workQueue = new();
      private readonly IObjectPool<Work> workPool = ObjectPool.CreateTlsBacked<Work>(pool => {
         var res = new Work();
         res.ReturnToPool = () => pool.ReturnObject(res);
         return res;
      });
      private readonly Semaphore workAvailableSignal = new(0, Int32.MaxValue);
      private readonly AsyncLatch shutdownLatch = new();
      private readonly List<IThreadInternal> workerThreads;
      private readonly HashSet<int> workerThreadIds;

      public CustomThreadPoolScheduler(IThreadFactory threadFactory, string name, int initialThreadCount) {
         workerThreads = Arrays.Create(
            initialThreadCount,
            i => threadFactory.Create(WorkerThreadStart, $"{name}_{i}")
                              .Tap(t => t.Start())
         ).ToList();
         workerThreadIds = workerThreads.Map(t => t.ManagedThreadId).ToHashSet();
      }

      public bool IsCurrentThreadInPool => workerThreadIds.Contains(Thread.CurrentThread.ManagedThreadId);

      private void WorkerThreadStart() {
         while (!shutdownLatch.IsSignalled) {
            try {
               var (action, state) = workQueue.WaitThenDequeue(workAvailableSignal);
               if (action == null) {
                  shutdownLatch.IsSignalled.AssertIsTrue();
                  return;
               }
               action(state);
            } catch (Exception e) {
               Console.Error.WriteLine("Error thrown at scheduler: " + e);
            }
         }
      }

      public void Shutdown() { }

      public void Schedule(Action<object> work, object state, Action callback) {
         workQueue.Enqueue((st => {
            work(st);
            callback?.Invoke();
         }, state));
         workAvailableSignal.Release();
      }

      public Task ExecuteAsync(Action<object> work, object state) {
         var latch = new AsyncLatch();
         Schedule(work, state, latch.SetOrThrow);
         return latch.WaitAsync();
      }

      public IJobQueue<TJob> CreateJobQueue<TJob>(Action<TJob> jobHandler) {
         return new DefaultJobQueue<TJob>(this, jobHandler);
      }

      public IRequestResponseJobQueue<TJobRequest, TJobResponse> CreateRequestResponseJobQueue<TJobRequest, TJobResponse>(Func<TJobRequest, TJobResponse> jobHandler) {
         return new DefaultRequestResponseJobQueue<TJobRequest, TJobResponse>(this, jobHandler);
      }
   }
}