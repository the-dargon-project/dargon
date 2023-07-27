using System;
using Dargon.Commons;
using Dargon.Commons.Collections;
using Dargon.Commons.Utilities;
using Dargon.Courier.StateReplicationTier.Primaries;
using Dargon.Courier.StateReplicationTier.States;

namespace Dargon.Courier.StateReplicationTier.Predictions {
   public abstract class PredictionStateView<TState, TSnapshot, TDelta, TOperations> : IStateView<TState>
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

      public PredictionStateView(IStateView<TState> baseStateView, TOperations ops) {
         this.baseStateView = baseStateView;
         this.ops = ops;
      }

      public bool IsReady => baseStateView.IsReady;
      public int Version => baseStateView.Version + versionOffset;
      public event StateViewUpdatedEvent Updated;

      public TState State {
         get {
            ProcessUpdates();
            return predictedState;
         }
      }

      public void ProcessUpdates() {
         if (lastBaseStateViewVersion == baseStateView.Version) {
            return;
         }

         if (predictedState == null) {
            predictedState = ops.CloneState(baseStateView.State);
            lastBaseStateViewVersion = baseStateView.Version;
            return;
         }

         ops.Copy(baseStateView.State, predictedState);
         
         var newPredictionsListBuilder = predictions.GetDefaultOfType();
         for (var i = 0; i < predictions.Count; i++) {
            var prediction = predictions[i];
            var flags = prediction.TryBuildDelta(predictedState, out var delta);
            if (delta != null) {
               ops.TryApplyDelta(predictedState, delta).AssertIsTrue();
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

         Updated?.Invoke();
         lastBaseStateViewVersion = baseStateView.Version;
      }

      public bool AddPrediction(IPredictionDeltaSource<TState, TDelta> prediction) {
         ProcessUpdates();
         var res = prediction.TryBuildDelta(predictedState, out var delta);
         if ((res & PredictionResultFlags.DropSelf) != 0) {
            // prediction immediately dropped
            return false;
         }

         ops.TryApplyDelta(predictedState, delta).AssertIsTrue();
         predictions.Add(prediction);
         versionOffset++;
         Updated?.Invoke();
         return true;
      }

      public bool RemovePrediction(IPredictionDeltaSource<TState, TDelta> prediction) {
         var success = predictions.Remove(prediction);
         if (success) {
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
         Updated?.Invoke();
      }
   }
}