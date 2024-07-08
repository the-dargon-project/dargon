using System.Threading.Tasks;
using Dargon.Courier.StateReplicationTier.States;

namespace Dargon.Courier.StateReplicationTier.Shared;

/// <summary>
/// Interface abstracting applying proposals to state.
///
/// In a server context, this is an ingest for client-side proposals,
/// which might or might not be rejected.
/// 
/// In a client context, this attempts to applies proposals onto the
/// local prediction stack. If that succeeds, the proposal is transmitted
/// to the server. If the server rejects the proposal, the client-side
/// prediction is rolled back.
///
/// How client-side prediction eventually reconciles with a
/// server-accepted proposal is TBD.
/// </summary>
public interface IProposer<TState, TSnapshot, TDelta, TProposal>
   where TState : class, IState
   where TSnapshot : IStateSnapshot
   where TDelta : class, IStateDelta
   where TProposal : class, IProposal<TState, TDelta> {
   Task<bool> TryApplyAsync(TProposal proposal);
}