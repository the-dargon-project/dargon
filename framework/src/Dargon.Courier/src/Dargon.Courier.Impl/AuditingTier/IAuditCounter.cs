﻿using System.Threading;

namespace Dargon.Courier.AuditingTier {
   public interface IAuditCounter {
      void Increment();
   }

   public class NullAuditCounter : IAuditCounter {
      public void Increment() { }
   }

   public class AuditCounterImpl : IAuditCounter {
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
