using Dargon.Courier.StateReplicationTier.States;

namespace Dargon.Courier.StateReplicationTier.Recorders {
   public class StateRecorder<TState, TSnapshot, TDelta> where TSnapshot : IStateSnapshot where TDelta : class, IStateDelta { }
}