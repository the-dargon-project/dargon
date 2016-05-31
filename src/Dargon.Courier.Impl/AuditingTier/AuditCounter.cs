using System.Threading;

namespace Dargon.Courier.AuditingTier {
   public class AuditCounter {
      private int count;

      public void Increment() {
         Interlocked.Increment(ref count);
      }

      public int GetAndReset() {
         int takenCount = count;
         Interlocked.Add(ref count, -takenCount);
         return takenCount;
      }
   }
}
