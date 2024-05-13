using System.Collections.Generic;
using System.Threading.Tasks;
using Dargon.Courier.StateReplicationTier.States;
using Dargon.Courier.StateReplicationTier.Vox;

namespace Dargon.Courier.StateReplicationTier.Primaries {
   public interface IStateSnapshotProviderService<TState, TSnapshot, TDelta>
      where TState : class, IState
      where TSnapshot : IStateSnapshot
      where TDelta : class, IStateDelta {
      Task<StateUpdateDto> GetOutOfBandSnapshotUpdateOfLatestStateAsync();
   }

   public class StateSnapshotProviderService<TState, TSnapshot, TDelta, TOperations> : IStateSnapshotProviderService<TState, TSnapshot, TDelta>
      where TState : class, IState
      where TSnapshot : IStateSnapshot
      where TDelta : class, IStateDelta
      where TOperations : IStateDeltaOperations<TState, TSnapshot, TDelta> {
      private readonly StateView<TState, TSnapshot, TDelta, TOperations> primaryStateView;
      private readonly StatePublisher<TState, TSnapshot, TDelta> statePublisher;

      public StateSnapshotProviderService(StateView<TState, TSnapshot, TDelta, TOperations> primaryStateView, StatePublisher<TState, TSnapshot, TDelta> statePublisher) {
         this.primaryStateView = primaryStateView;
         this.statePublisher = statePublisher;
      }

      public async Task<StateUpdateDto> GetOutOfBandSnapshotUpdateOfLatestStateAsync() {
         return await statePublisher.CreateOutOfBandCatchupSnapshotUpdateAsync();
      }
   }
}