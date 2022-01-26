using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Dargon.Commons.AsyncAwait {
   /// <summary>
   /// See comments in <seealso cref="IAwaiterVoid"/>
   /// </summary>
   [EditorBrowsable(EditorBrowsableState.Never)]
   public interface IAwaiterCommonInternalDoNotExtend : INotifyCompletion {
      /// <summary>
      /// IsCompleted indicates whether the abstract task-like is already complete. If so the async function
      /// can continue executing rather than suspending and queuing a continuation via OnCompleted.
      /// </summary>
      bool IsCompleted { get; }

      /// <summary>
      /// OnCompleted schedules a continuation Action that runs when the task-like completes.
      /// The spec seemingly leaves it undefined what OnCompleted does when IsCompleted was already true.
      /// In practice, the continuation should then be run immediately or scheduled to run; not dropped.
      /// </summary>
      new void OnCompleted(Action continuation);
   }
}