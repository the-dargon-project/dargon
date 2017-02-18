﻿namespace NMockito.Counters {
   public interface Counter {
      int Remaining { get; }
      void HandleVerified(int count);
      bool IsSatisfied { get; }
      string Description { get; }
   }
}
