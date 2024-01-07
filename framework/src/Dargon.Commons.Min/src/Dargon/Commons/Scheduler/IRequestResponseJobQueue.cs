using System;
using System.Threading.Tasks;

namespace Dargon.Commons.Scheduler {
   public interface IRequestResponseJobQueue<TJobRequest, TJobResponse> {
      void EnqueueWithCallback(TJobRequest request, Action<TJobResponse> callback);
      Task<TJobResponse> EnqueueAndAwaitAsync(TJobRequest request);
   }
}