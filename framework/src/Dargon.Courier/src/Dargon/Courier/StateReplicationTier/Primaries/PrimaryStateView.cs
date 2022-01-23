using Dargon.Commons;
using Dargon.Commons.Utilities;
using Dargon.Courier.StateReplicationTier.States;

namespace Dargon.Courier.StateReplicationTier.Primaries {
   public class PrimaryStateView<TState, TSnapshot, TDelta, TOperations> : IStateView<TState>
      where TState : class, IState
      where TSnapshot : IStateSnapshot
      where TDelta : class, IStateDelta
      where TOperations : IStateDeltaOperations<TState, TSnapshot, TDelta> {

      private readonly TState state;
      private readonly TOperations ops;
      private readonly StatePublisher<TState, TSnapshot, TDelta, TOperations> publisher;
      private int version = 0;

      public PrimaryStateView(TState state, TOperations ops, StatePublisher<TState, TSnapshot, TDelta, TOperations> publisher) {
         this.state = state;
         this.ops = ops;
         this.publisher = publisher;
      }

      public int Version => version;
      public TState State => state;

      public void ApplyDeltaOrThrow(TDelta delta) => TryApplyDelta(delta).AssertIsTrue();

      public bool TryApplyDelta(TDelta delta) {
         var success = ops.TryApplyDelta(state, delta);

         if (success) {
            // immediately queues but doesn't await network sends
            // out-of-order arrivals will be handled by the seq number.
            publisher.QueueDeltaPublishAsync(delta).Forget();
         }

         return success;
      }
   }
}
