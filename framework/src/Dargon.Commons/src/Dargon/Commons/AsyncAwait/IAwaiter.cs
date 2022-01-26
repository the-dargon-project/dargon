namespace Dargon.Commons.AsyncAwait {
   /// <summary>
   /// See comments in <seealso cref="IAwaiterVoid"/>
   /// </summary>
   public interface IAwaiter<out T> : IAwaiterCommonInternalDoNotExtend {
      /// <summary>
      /// See comments in <seealso cref="IAwaiterVoid.GetResult"/>
      /// </summary>
      T GetResult();
   }
}