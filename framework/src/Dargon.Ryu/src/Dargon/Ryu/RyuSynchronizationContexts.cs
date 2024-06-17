using System.Threading;
using Dargon.Commons.AsyncPrimitives;

namespace Dargon.Ryu {
   public sealed class RyuSynchronizationContexts {
      public required SingleThreadedSynchronizationContext MainThread;
      public required SynchronizationContext BackgroundThreadPool;
   }
}
