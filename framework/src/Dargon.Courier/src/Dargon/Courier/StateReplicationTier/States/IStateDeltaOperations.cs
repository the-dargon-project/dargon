using Dargon.Commons;

namespace Dargon.Courier.StateReplicationTier.States {
   public interface IStateDeltaOperations<TState, TSnapshot, TDelta> : IStateSnapshotOperations<TState, TSnapshot> where TState : IState where TSnapshot : IStateSnapshot where TDelta : IStateDelta {
      /// <summary>
      /// Attempts to apply the delta.
      /// </summary>
      /// <returns>Whether the delta successfully applied. If false, the state must remain unchanged.</returns>
      bool TryApplyDelta(TState state, TDelta delta);

      void ApplyDeltaOrThrow(TState state, TDelta delta) {
         TryApplyDelta(state, delta).AssertIsTrue();
      }
   }
}