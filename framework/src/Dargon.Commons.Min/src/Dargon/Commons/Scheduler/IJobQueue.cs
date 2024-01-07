using System;
using System.Threading.Tasks;

namespace Dargon.Commons.Scheduler {
   public interface IJobQueue<TJobData> {
      void Enqueue(TJobData data);
      void EnqueueWithCallback(TJobData data, Action callback);
      Task EnqueueAndAwaitAsync(TJobData data);
   }
}