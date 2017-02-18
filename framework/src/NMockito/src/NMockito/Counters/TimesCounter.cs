using System.Threading;

namespace NMockito.Counters {
   public class TimesCounter : Counter {
      private int remaining;

      public TimesCounter(int count) {
         remaining = count;
      }

      public int Remaining => remaining;
      public bool IsSatisfied => remaining == 0;
      public string Description => remaining.ToString();

      public void HandleVerified(int count) {
         Interlocked.Add(ref remaining, -count);
      }
   }
}