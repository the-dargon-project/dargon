using System;
using Dargon.Commons;
using Dargon.Commons.Collections;
using Dargon.Courier.StateReplicationTier.Primaries;
using Dargon.Courier.StateReplicationTier.States;

namespace Dargon.Courier.StateReplicationTier.Predictions {
   public class StatePredictor<TState, TSnapshot, TDelta> where TState : class, IState where TSnapshot : IStateSnapshot where TDelta : class, IStateDelta {
      private const int kInvalidatedBaseStateViewVersion = int.MinValue;
      
      private readonly IStateView<TState, TSnapshot, TDelta> baseStateView;
      private readonly IStateView<TState, TSnapshot, TDelta> predictionStateView;

      private ExposedArrayList<IPredictionDeltaSource<TState, TDelta>> predictions = new();
      private ExposedArrayList<IPredictionDeltaSource<TState, TDelta>> preallocatedPredictionsAlternateList = new();
      private int lastBaseStateViewVersion = kInvalidatedBaseStateViewVersion;

      private ReplicationVersion replicationVersion;

      public StatePredictor(IStateView<TState, TSnapshot, TDelta> baseStateView, IStateView<TState, TSnapshot, TDelta> predictionStateView) {
         this.baseStateView = baseStateView;
         this.predictionStateView = predictionStateView;
      }

      public SyncStateGuard<TState> ProcessUpdatesAndKeepWriterLock() {
         var psvGuard = predictionStateView.LockStateForWrite();
         ProcessUpdates();
         return psvGuard;
      }

      public void ProcessUpdates() {
         if (lastBaseStateViewVersion == baseStateView.Version) {
            return;
         }

         using var bsvGuard = baseStateView.LockStateForRead();
         using var psvGuard = predictionStateView.LockStateForWrite();
         predictionStateView.Copy(bsvGuard.State, IncrementReplicationVersion());
         bsvGuard.Dispose();

         var predictedState = psvGuard.State;
         var newPredictionsListBuilder = predictions.GetDefaultOfType();
         for (var i = 0; i < predictions.Count; i++) {
            var prediction = predictions[i];
            var flags = prediction.TryBuildDelta(predictedState, out var delta);
            if (delta != null) {
               predictionStateView.TryApplyDelta(delta).AssertIsTrue();
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

         lastBaseStateViewVersion = baseStateView.Version;
      }

      public bool AddPrediction(IPredictionDeltaSource<TState, TDelta> prediction) {
         using var psvGuard = ProcessUpdatesAndKeepWriterLock();

         var predictedState = psvGuard.State;
         var res = prediction.TryBuildDelta(predictedState, out var delta);
         if ((res & PredictionResultFlags.DropSelf) != 0) {
            // prediction immediately dropped
            return false;
         }

         predictionStateView.TryApplyDelta(delta).AssertIsTrue();
         IncrementReplicationVersion();

         predictions.Add(prediction);
         return true;
      }

      public bool RemovePrediction(IPredictionDeltaSource<TState, TDelta> prediction) {
         using var psvGuard = predictionStateView.LockStateForWrite();

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
      }

      private ReplicationVersion IncrementReplicationVersion() {
         replicationVersion.Seq++;
         if (replicationVersion.Seq == int.MaxValue) {
            replicationVersion.Epoch++;
            replicationVersion.Seq = 0;
         }
         return replicationVersion;
      }
   }
}