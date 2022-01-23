using System;
using Dargon.Commons;
using Dargon.Commons.Collections;
using Dargon.Commons.Utilities;
using Dargon.Courier.StateReplicationTier.Primaries;
using Dargon.Courier.StateReplicationTier.States;

namespace Dargon.Courier.StateReplicationTier.Predictions {
   public abstract class PredictedStateView<TState, TSnapshot, TDelta, TOperations> : IStateView<TState>
      where TState : class, IState
      where TSnapshot : IStateSnapshot
      where TDelta : class, IStateDelta
      where TOperations : IStateDeltaOperations<TState, TSnapshot, TDelta> {
      private const int kInvalidatedBaseStateViewVersion = int.MinValue;

      private readonly IStateView<TState> baseStateView;
      private readonly TOperations ops;
      private ExposedArrayList<IPredictionDeltaSource<TState, TDelta>> predictions = new();
      private ExposedArrayList<IPredictionDeltaSource<TState, TDelta>> preallocatedPredictionsAlternateList = new();
      private int versionOffset = 0;
      private int lastBaseStateViewVersion = kInvalidatedBaseStateViewVersion;
      private TState predictedState;

      public PredictedStateView(IStateView<TState> baseStateView, TOperations ops) {
         this.baseStateView = baseStateView;
         this.ops = ops;
      }

      public int Version => baseStateView.Version + versionOffset;

      public TState State {
         get {
            if (predictions.Count == 0) {
               return baseStateView.State;
            }

            return RecomputePredictedStateIfBaseChanged();
         }
      }

      private TState RecomputePredictedStateIfBaseChanged() {
         if (lastBaseStateViewVersion == baseStateView.Version) {
            return predictedState;
         }

         var newPredictedState = ops.CloneState(baseStateView.State);
         var newPredictionsListBuilder = predictions.GetDefaultOfType();
         for (var i = 0; i < predictions.Count; i++) {
            var prediction = predictions[i];
            var flags = prediction.TryBuildDelta(newPredictedState, out var delta);
            if (delta != null) {
               ops.TryApplyDelta(newPredictedState, delta).AssertIsTrue();
            }

            var hasDropFlag = flags.FastHasAnyFlag(PredictionResultFlags.__InternalAnyDropFlagMask);
            if (hasDropFlag && newPredictionsListBuilder == null) {
               newPredictionsListBuilder = preallocatedPredictionsAlternateList;
               newPredictionsListBuilder.Count.AssertEquals(0);
               newPredictionsListBuilder.AddRange(predictions.store, 0, i);
            }

            if (newPredictionsListBuilder != null && !flags.FastHasFlag(PredictionResultFlags.DropSelf)) {
               newPredictionsListBuilder.Add(prediction);
            }

            if (flags.FastHasFlag(PredictionResultFlags.IgnoreSuccessors)) {
               if (!flags.FastHasFlag(PredictionResultFlags.DropSuccessors)) {
                  newPredictionsListBuilder!.AddRange(predictions.store.AsSpan(i + 1));
               }
               break;
            }
         }

         if (newPredictionsListBuilder != null) {
            (predictions, preallocatedPredictionsAlternateList) = (preallocatedPredictionsAlternateList, predictions);
            preallocatedPredictionsAlternateList.Clear();
         }

         predictedState = newPredictedState;
         return predictedState;
      }

      public bool AddPrediction(IPredictionDeltaSource<TState, TDelta> prediction) {
         RecomputePredictedStateIfBaseChanged().AssertReferenceEquals(predictedState);
         var res = prediction.TryBuildDelta(predictedState, out var delta);
         if ((res & PredictionResultFlags.DropSelf) != 0) {
            // prediction immediately dropped
            return false;
         }

         ops.TryApplyDelta(predictedState, delta).AssertIsTrue();
         predictions.Add(prediction);
         versionOffset++;
         return true;
      }

      public bool RemovePrediction(IPredictionDeltaSource<TState, TDelta> prediction) {
         var success = predictions.Remove(prediction);
         if (success) {
            versionOffset++;
            Invalidate();
         }

         return success;
      }

      /// <summary>
      /// Flags the current predicted state as dirty, forcing a recompute when
      /// state is next requested.
      /// </summary>
      public void Invalidate() {
         lastBaseStateViewVersion = kInvalidatedBaseStateViewVersion;
         versionOffset++;
      }
   }
}