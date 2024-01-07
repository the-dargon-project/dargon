using System;
using System.Threading.Tasks;
using Dargon.Commons.AsyncPrimitives;

namespace Dargon.Commons.Scheduler {
   public static class SchedulerExtensions {
      public static void Schedule(this IScheduler scheduler, Action work) {
         scheduler.Schedule(_ => work(), null);
      }

      public static Task ExecuteAsync(this IScheduler scheduler, Action work) {
         return scheduler.ExecuteAsync(_ => work(), null);
      }

      public static Task<T> ExecuteAsync<T>(this IScheduler scheduler, Func<T> work) {
         var res = new AsyncBox<T>();
         scheduler.ExecuteAsync(_ => res.SetResult(work()), null);
         return res.GetResultAsync();
      }
   }
}