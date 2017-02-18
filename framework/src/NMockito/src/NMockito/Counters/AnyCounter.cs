namespace NMockito.Counters {
   public class AnyCounter : Counter {
      private bool isSatisfied;

      public int Remaining => int.MaxValue;
      public bool IsSatisfied => isSatisfied;
      public string Description => "Any";

      public void HandleVerified(int count) {
         isSatisfied = true;
      }
   }
}