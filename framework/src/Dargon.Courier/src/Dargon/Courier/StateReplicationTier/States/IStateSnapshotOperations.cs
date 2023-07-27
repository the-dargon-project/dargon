namespace Dargon.Courier.StateReplicationTier.States {
   public interface IStateSnapshotOperations<TState, TSnapshot> where TSnapshot : IStateSnapshot {
      public TState CreateState();

      public TState CloneState(TState state) {
         var clone = CreateState();
         LoadSnapshot(clone, CaptureSnapshot(state));
         return clone;
      }

      public TSnapshot CaptureSnapshot(TState state);
      public void LoadSnapshot(TState state, TSnapshot snapshot);
      public void Copy(TState src, TState dest);
   }
}