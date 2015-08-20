using System;

namespace Dargon.Courier.Peering {
   public interface RevisionCounter {
      bool TryAdvance(int nextCount);
      bool IsCurrentCount(int value);
      int GetCurrentCount();
   }

   public class RevisionCounterImpl : RevisionCounter {
      private readonly object synchronization = new object();
      private bool isFirstCall = true;
      private int count = 0;

      public bool TryAdvance(int nextCount) {
         lock (synchronization) {
            if (isFirstCall) {
               isFirstCall = false;
               count = nextCount;
               return true;
            }

            if (nextCount < Int32.MinValue / 2 && count > Int32.MaxValue / 2) {
               // wrap-around case
               count = nextCount;
               return true;
            } else if (nextCount > count) {
               // normal increment case
               count = nextCount;
               return true;
            } else {
               // Same count indicates no revision change
               return false;
            }
         }
      }

      public bool IsCurrentCount(int value) {
         lock (synchronization) {
            return count == value;
         }
      }

      public int GetCurrentCount() {
         lock (synchronization) {
            return count;
         }
      }
   }
}