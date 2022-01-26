using System;
using System.Threading.Tasks;

namespace Dargon.Commons.Scheduler {
   public interface IScheduler {
      void Schedule(Action<object> work, object state, Action callback = null);
      Task ExecuteAsync(Action<object> work, object state);

      IJobQueue<TJob> CreateJobQueue<TJob>(Action<TJob> jobHandler);
      IRequestResponseJobQueue<TJobRequest, TJobResponse> CreateRequestResponseJobQueue<TJobRequest, TJobResponse>(Func<TJobRequest, TJobResponse> jobHandler);
   }
}