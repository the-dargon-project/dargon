namespace Dargon.Commons.AsyncAwait {
   /// <summary>
   /// See comments in <seealso cref="IAwaiterVoid"/>
   ///
   /// Note that to the compiler IAwaitable and IAwaiter are duck-typed.
   /// In fact, IAwaitable.GetAwaiter() can even be implemented via an extension method.
   ///
   /// <seealso cref="SynchronizationContextExtensions.GetAwaiter"/>
   /// </summary>
   public interface IAwaitable<TAwaiter> {
      TAwaiter GetAwaiter();
   }
}