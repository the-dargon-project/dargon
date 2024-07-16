using System;

namespace Dargon.Courier.StateReplicationTier.States {
   public interface IStateDelta {
      Guid ProposalIdOpt { get; }
   }
}