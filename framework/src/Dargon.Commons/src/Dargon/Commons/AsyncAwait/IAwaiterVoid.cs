namespace Dargon.Commons.AsyncAwait {
   /// <summary>
   /// An awaitable type must declare a method
   ///
   ///   TAwaiter GetAwaiter() where TAwaiter implements the methods described in this interface.
   ///
   /// This awaitable type is referred to vaguely as a "task" (not to be confused with Task/Task-of-T).
   ///
   ///     It represents an asynchronous operation that may or may not be complete at the
   ///     time the await-expression is evaluated. The purpose of the await operator is to
   ///     suspend execution of the enclosing async function until the awaited task is
   ///     complete, and then obtain its outcome.
   ///
   /// Note that to the compiler, this interface is duck-typed; it does not need to be implemented,
   /// and can even be implemented via extension methods.
   ///
   /// See https://devblogs.microsoft.com/pfxteam/await-anything/
   /// See https://www.jacksondunstan.com/articles/4918
   /// </summary>
   public interface IAwaiterVoid : IAwaiterCommonInternalDoNotExtend {
      /// <summary>
      /// Gets the result of the task, blocking if necessary
      /// </summary>
      void GetResult();
   }
}