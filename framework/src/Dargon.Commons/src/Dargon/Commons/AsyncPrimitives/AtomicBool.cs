using System.Threading;

namespace Dargon.Commons.AsyncPrimitives {
   public struct AtomicBool {
      private int val = 0;

      public AtomicBool(bool val) => this.val = val ? 1 : 0;

      public bool Value {
         get => Interlocked2.Read(ref val) != 0;
         set => Interlocked2.Write(ref val, value ? 1 : 0);
      }

      public bool TrySetToTrueFromFalse() => Interlocked.CompareExchange(ref val, 1, 0) == 0;
   }

   public struct AtomicLatch {
      private AtomicBool inner;

      public bool IsSet => inner.Value;
      public bool TrySetOnce() => inner.TrySetToTrueFromFalse();
   }
}
