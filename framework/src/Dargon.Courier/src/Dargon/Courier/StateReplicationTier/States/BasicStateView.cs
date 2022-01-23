using Dargon.Commons;

namespace Dargon.Courier.StateReplicationTier.States {
   public class BasicStateView<TState, TSnapshot, TDelta> : IStateView<TState>
      where TState : class, IState
      where TSnapshot : IStateSnapshot
      where TDelta : class, IStateDelta {

      protected readonly TState state;
      protected readonly IStateDeltaOperations<TState, TSnapshot, TDelta> ops;
      private readonly object sync = new();
      private int version;

      public BasicStateView(TState state, IStateDeltaOperations<TState, TSnapshot, TDelta> ops) {
         this.state = state;
         this.ops = ops;
      }

      public TState State => state;
      public int Version => Interlocked2.Read(ref version);

      public void LoadSnapshot(TSnapshot snapshot) {
         lock (sync) {
            ops.LoadSnapshot(state, snapshot);
            version++;
         }
      }

      public TSnapshot CaptureSnapshot() {
         return ops.CaptureSnapshot(state);
      }

      public bool TryApplyDelta(TDelta delta) {
         var success = ops.TryApplyDelta(state, delta);
         return success;
      }
   }
}