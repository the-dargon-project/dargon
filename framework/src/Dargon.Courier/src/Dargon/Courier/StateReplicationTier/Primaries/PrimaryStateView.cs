using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Commons.Utilities;
using Dargon.Courier.StateReplicationTier.States;
using Dargon.Courier.StateReplicationTier.Vox;

namespace Dargon.Courier.StateReplicationTier.Primaries {
   public class PrimaryStateView<TState, TSnapshot, TDelta, TOperations> : IStateView<TState>
      where TState : class, IState
      where TSnapshot : IStateSnapshot
      where TDelta : class, IStateDelta
      where TOperations : IStateDeltaOperations<TState, TSnapshot, TDelta> {

      private readonly TState state;
      private readonly TOperations ops;
      private readonly StatePublisher<TState, TSnapshot, TDelta, TOperations> publisher;
      private readonly StateLock stateLock;
      private int version = 0;

      public PrimaryStateView(TState state, TOperations ops, StatePublisher<TState, TSnapshot, TDelta, TOperations> publisher, StateLock stateLock) {
         this.state = state;
         this.ops = ops;
         this.publisher = publisher;
         this.stateLock = stateLock;
      }

      public int Version => version;
      public TState State => state;
      public bool IsReady => true;
      public event StateViewUpdatedEvent Updated;

      public void ApplyDeltaOrThrow(TDelta delta) => TryApplyDelta(delta).AssertIsTrue();

      public bool TryApplyDelta(TDelta delta) {
         var success = ops.TryApplyDelta(state, delta);

         if (success) {
            version++;

            // immediately queues but doesn't await network sends
            // out-of-order arrivals will be handled by the seq number.
            publisher.QueueDeltaPublishAsync(delta).Forget();
            Updated?.Invoke();
         }

         return success;
      }
   }

   public interface IPrimaryStateService<TState, TSnapshot, TDelta>
      where TState : class, IState
      where TSnapshot : IStateSnapshot
      where TDelta : class, IStateDelta {
      Task<StateUpdateDto> GetOutOfBandSnapshotUpdateOfLatestStateAsync();
   }

   public class PrimaryStateService<TState, TSnapshot, TDelta, TOperations> : IPrimaryStateService<TState, TSnapshot, TDelta>
      where TState : class, IState
      where TSnapshot : IStateSnapshot
      where TDelta : class, IStateDelta 
      where TOperations : IStateDeltaOperations<TState, TSnapshot, TDelta> {
      private readonly PrimaryStateView<TState, TSnapshot, TDelta, TOperations> primaryStateView;
      private readonly TOperations ops;
      private readonly StatePublisher<TState, TSnapshot, TDelta, TOperations> statePublisher;
      private readonly StateLock stateLock;

      public PrimaryStateService(PrimaryStateView<TState, TSnapshot, TDelta, TOperations> primaryStateView, TOperations ops, StatePublisher<TState, TSnapshot, TDelta, TOperations> statePublisher, StateLock stateLock) {
         this.primaryStateView = primaryStateView;
         this.ops = ops;
         this.statePublisher = statePublisher;
         this.stateLock = stateLock;
      }

      public async Task<StateUpdateDto> GetOutOfBandSnapshotUpdateOfLatestStateAsync() {
         await using var mut = await stateLock.CreateReaderGuardAsync();
         var snapshot = ops.CaptureSnapshot(primaryStateView.State);
         var update = await statePublisher.CreateOutOfBandCatchupSnapshotUpdateAsync(snapshot);
         return update;
      }
   }
}
