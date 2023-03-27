using System;
using System.Threading.Tasks;
using Dargon.Commons.Utilities;

namespace Dargon.Courier.Utilities {
   public static class TaskUtilities {
      private delegate Task CastTaskReturnTypeHelperFunc(Task t);

      private static readonly IGenericFlyweightFactory<CastTaskReturnTypeHelperFunc> castTaskReturnTypeFuncs
         = GenericFlyweightFactory.ForStaticMethod<CastTaskReturnTypeHelperFunc>(
            typeof(TaskUtilities),
            nameof(CastTaskReturnTypeHelper));


      public static Task CastTask(Task t, Type destTaskType) {
         if (destTaskType == typeof(Task)) {
            return t;
         }
         var destTaskResultType = destTaskType.GetGenericArguments()[0];
         return castTaskReturnTypeFuncs.Get(destTaskResultType)(t);
      }

      private static async Task<TDestTaskResult> CastTaskReturnTypeHelper<TDestTaskResult>(Task t) {
         return (TDestTaskResult)await GetTaskResultAsync(t);
      }

      public static Type UnboxTypeIfTask(Type t) {
         if (!typeof(Task).IsAssignableFrom(t))
            return t;
         return t.GetGenericArguments()[0];
      }

      public static async Task<object> UnboxValueIfTaskAsync(object value) {
         var task = value as Task;
         if (task != null) {
            return await GetTaskResultAsync(task).ConfigureAwait(false);
         }
         return value;
      }

      public static async Task<object> GetTaskResultAsync(Task task) {
         object result = null;
         await task.ConfigureAwait(false);
         if (task.GetType().IsGenericType) {
            var taskResult = task.GetType().GetProperty("Result").GetValue(task);

            if (taskResult != null && taskResult.GetType().Name != "VoidTaskResult") {
               result = taskResult;
            }
         }
         return result;
      }
   }
}
