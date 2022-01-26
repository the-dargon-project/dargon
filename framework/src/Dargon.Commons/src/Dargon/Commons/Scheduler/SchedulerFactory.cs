using System;

namespace Dargon.Commons.Scheduler {
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
}