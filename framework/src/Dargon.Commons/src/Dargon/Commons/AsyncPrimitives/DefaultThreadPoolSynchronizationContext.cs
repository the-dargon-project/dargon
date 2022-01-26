using System.Threading;

namespace Dargon.Commons.AsyncPrimitives {
   /// <summary>
   /// SynchronizationContext by default queues tasks to the thread pool via
   /// <see cref="ThreadPool.QueueUserWorkItem(System.Threading.WaitCallback)"/>
   /// </summary>
   public class DefaultThreadPoolSynchronizationContext : SynchronizationContext {
      private static DefaultThreadPoolSynchronizationContext s_instance = new DefaultThreadPoolSynchronizationContext();

      public static DefaultThreadPoolSynchronizationContext Instance => s_instance;

      private DefaultThreadPoolSynchronizationContext() {}
   }
}