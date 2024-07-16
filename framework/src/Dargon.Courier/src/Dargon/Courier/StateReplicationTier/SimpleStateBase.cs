using System;
using Dargon.Courier.StateReplicationTier.States;
using Dargon.Courier.StateReplicationTier.Utils;

namespace Dargon.Courier.StateReplicationTier;

public abstract class SimpleStateBase<TSelf> : StateBase<TSelf, TSelf, SimpleStateBase<TSelf>.Delta>, IStateSnapshot
   where TSelf : SimpleStateBase<TSelf>, IState, IStateSnapshot, new() {

   public abstract TSelf Clone();
   public abstract void LoadFrom(TSelf other);

   public class Delta : IStateDelta {
      public Guid ProposalIdOpt { get; set; }
   }

   public class Ops : IOperations {
      public TSelf CreateState() => new TSelf();
      public TSelf CaptureSnapshot(TSelf state) => state.Clone();
      public void LoadSnapshot(TSelf state, TSelf snapshot) => state.LoadFrom(snapshot);
      public void Copy(TSelf src, TSelf dest) => dest.LoadFrom(src);
      public bool TryApplyDelta(TSelf state, Delta delta) => throw new NotImplementedException();
   }
}