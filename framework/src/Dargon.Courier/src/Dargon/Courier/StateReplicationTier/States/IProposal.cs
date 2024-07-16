using System;

namespace Dargon.Courier.StateReplicationTier.States {
   public interface IProposal { }

   public interface IProposal<TState, TDelta> : IProposal
      where TState: class, IState
      where TDelta : class, IStateDelta {

      Guid ProposalId { get; }

      /// <summary>
      /// Builds a delta off the given state, which must be treated as immutable.
      ///
      /// This must be a pure function of the given state; a given state and delta factory
      /// source should probably always map to the same outcome.
      ///
      /// If you do choose to mutate the delta source and are working with a predictor,
      /// you must invalidate the prediction state view to force it to recompute the
      /// predicted state. (though in practice this should be seen as undefined behavior)...
      /// </summary>
      Result TryBuildDelta(TState state, out TDelta delta);
   }

   [Flags]
   public enum Result {
      /// <summary>
      /// The default operation is to apply the delta (or do nothing if it is null)
      /// and keep the prediction in the predictions list.
      /// </summary>
      Default = 0,
      Ok = Default,

      /// <summary>
      /// Further predictions should not execute.
      /// </summary>
      IgnoreSuccessors = 1 << 0,

      /// <summary>
      /// Do not use. Is DropSuccessors without the IgnoreSuccessors flag.
      /// </summary>
      __InternalDropSuccessors = 1 << 1,

      /// <summary>
      /// Further predictions of overlapping mask should be dropped from the prediction list.
      /// For example, if a step in movement prediction fails to rebase, further movement steps
      /// might not make sense to process (preferring a rubber-band).
      /// 
      /// Implicitly includes <see cref="IgnoreSuccessors"/>
      /// </summary>
      DropSuccessors = __InternalDropSuccessors | IgnoreSuccessors,

      /// <summary>
      /// The current prediction should be removed from the prediction list,
      /// or in other words, the prediction/deltaFactory cannot be applied to
      /// the current state.
      /// </summary>
      DropSelf = 1 << 2,

      __InternalAnyDropFlagMask = __InternalDropSuccessors | DropSelf,
   }
}