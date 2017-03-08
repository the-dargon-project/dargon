using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons.AsyncPrimitives;

namespace Dargon.Commons.Scheduler {
   public interface IJobQueue<TJobData> {
      void Enqueue(TJobData data);
      void EnqueueWithCallback(TJobData data, Action callback);
      Task EnqueueAndAwaitAsync(TJobData data);
   }

   public interface IRequestResponseJobQueue<TJobRequest, TJobResponse> {
      void EnqueueWithCallback(TJobRequest request, Action<TJobResponse> callback);
      Task<TJobResponse> EnqueueAndAwaitAsync(TJobRequest request);
   }

   public interface IScheduler {
      void Schedule(Action work);
      void Schedule(Action work, Action callback);
      void Schedule<T>(Func<T> work, Action<T> callback);

      Task ExecuteAsync(Action work);
      Task<T> ExecuteAsync<T>(Func<T> work);

      IJobQueue<TJob> CreateJobQueue<TJob>(Action<TJob> jobHandler);
      IRequestResponseJobQueue<TJobRequest, TJobResponse> CreateRequestResponseJobQueue<TJobRequest, TJobResponse>(Func<TJobRequest, TJobResponse> jobHandler);
   }

   public class SchedulerFactory {
      private readonly IThreadFactory threadFactory;

      public SchedulerFactory(IThreadFactory threadFactory) {
         this.threadFactory = threadFactory;
      }

      public IScheduler CreateWithCustomThreadPool(string name) {
         return new CustomThreadPoolScheduler(threadFactory, name, Environment.ProcessorCount);
      }

      public IScheduler CreateWithCustomThreadPool(string name, int initialThreadCount) {
         return new CustomThreadPoolScheduler(threadFactory, name, initialThreadCount);
      }
   }

   public interface IThread {
      void Start();
   }

   public interface IThreadFactory {
      IThread Create(Action threadStart, string name = null);
   }

   public class CustomThreadPoolScheduler : IScheduler {
      private readonly ConcurrentQueue<Action> workQueue = new ConcurrentQueue<Action>();
      private readonly Semaphore workAvailableSignal = new Semaphore(0, Int32.MaxValue);
      private readonly IReadOnlyList<IThread> workerThreads;

      public CustomThreadPoolScheduler(IThreadFactory threadFactory, string name, int initialThreadCount) {
         workerThreads = new List<IThread>(Arrays.Create(initialThreadCount, i => threadFactory.Create(WorkerThreadStart, $"{name}_{i}")));
         workerThreads.ForEach(t => t.Start());
      }

      private void WorkerThreadStart() {
         while (true) {
            try {
               workQueue.WaitThenDequeue(workAvailableSignal)();
            } catch (Exception e) {
               Console.Error.WriteLine("Error thrown at scheduler: " + e);
            }
         }
      }

      public void Schedule(Action work) {
         workQueue.Enqueue(work);
         workAvailableSignal.Release();
      }

      public void Schedule(Action work, Action callback) {
         Action combinedWork = null;
         combinedWork += work;
         combinedWork += callback;

         workQueue.Enqueue(combinedWork);
      }

      public void Schedule<T>(Func<T> work, Action<T> callback) {
         Schedule(() => callback(work()));
      }

      public Task ExecuteAsync(Action work) {
         var latch = new AsyncLatch();
         Schedule(work, latch.SetOrThrow);
         return latch.WaitAsync();
      }

      public Task<T> ExecuteAsync<T>(Func<T> work) {
         var box = new AsyncBox<T>();
         Schedule(work, box.SetResult);
         return box.GetResultAsync();
      }

      public IJobQueue<TJob> CreateJobQueue<TJob>(Action<TJob> jobHandler) {
         return new DefaultJobQueue<TJob>(this, jobHandler);
      }

      public IRequestResponseJobQueue<TJobRequest, TJobResponse> CreateRequestResponseJobQueue<TJobRequest, TJobResponse>(Func<TJobRequest, TJobResponse> jobHandler) {
         return new DefaultRequestResponseJobQueue<TJobRequest, TJobResponse>(this, jobHandler);
      }
   }

   public class DefaultJobQueue<TJobData> : IJobQueue<TJobData> {
      private readonly IScheduler scheduler;
      private readonly Action<TJobData> jobHandler;

      public DefaultJobQueue(IScheduler scheduler, Action<TJobData> jobHandler) {
         this.scheduler = scheduler;
         this.jobHandler = jobHandler;
      }

      public void Enqueue(TJobData data) {
         scheduler.Schedule(() => jobHandler(data));
      }

      public void EnqueueWithCallback(TJobData data, Action callback) {
         scheduler.Schedule(() => jobHandler(data), callback);
      }

      public Task EnqueueAndAwaitAsync(TJobData data) {
         return scheduler.ExecuteAsync(() => jobHandler(data));
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
         scheduler.Schedule(() => jobHandler(request), callback);
      }

      public Task<TJobResponse> EnqueueAndAwaitAsync(TJobRequest request) {
         return scheduler.ExecuteAsync(() => jobHandler(request));
      }
   }
}
