using Dargon.Courier.StateReplicationTier.States;

namespace Dargon.Courier.StateReplicationTier.Predictions {
   public interface IPredictionDeltaSource<TState, TDelta> where TDelta : class, IStateDelta {
      /// <summary>
      /// Builds a delta off the given state, which must be treated as immutable.
      ///
      /// This must be a pure function of the given state; a given state and prediction delta
      /// source should probably always map to the same outcome.
      ///
      /// If you do choose to mutate the delta source, you must invalidate the prediction state view
      /// to force it to recompute the predicted state.
      /// </summary>
      PredictionResultFlags TryBuildDelta(TState state, out TDelta delta);
   }
}