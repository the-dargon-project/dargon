using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Commons.Utilities;
using Dargon.Courier.StateReplicationTier.States;

namespace Dargon.Courier.StateReplicationTier.Replicas {
   public class ReplicaStateView<TState, TSnapshot, TDelta> : IStateView<TState>
      where TState : class, IState
      where TSnapshot : IStateSnapshot
      where TDelta : class, IStateDelta {
      
      private readonly TState state;
      private readonly RemoteStateSubscriber<TState, TSnapshot, TDelta> remoteStateSubscriber;
      private readonly StateUpdateProcessor<TState, TSnapshot, TDelta> stateUpdateProcessor;

      public ReplicaStateView(TState state, RemoteStateSubscriber<TState, TSnapshot, TDelta> remoteStateSubscriber, StateUpdateProcessor<TState, TSnapshot, TDelta> stateUpdateProcessor) {
         this.state = state;
         this.remoteStateSubscriber = remoteStateSubscriber;
         this.stateUpdateProcessor = stateUpdateProcessor;
      }

      public Task WaitForAndProcessInitialStateUpdateAsync() => stateUpdateProcessor.WaitForAndProcessInitialStateUpdateAsync();

      public void ProcessUpdates() {
         if (stateUpdateProcessor.HasInboundUpdates) {
            stateUpdateProcessor.ProcessQueuedUpdates();
         }
      }

      public int Version => stateUpdateProcessor.Version;
      public TState State => state;
   }
}
