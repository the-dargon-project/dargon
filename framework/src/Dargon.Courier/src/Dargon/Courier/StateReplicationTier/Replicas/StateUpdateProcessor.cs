using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Commons.AsyncPrimitives;
using Dargon.Courier.StateReplicationTier.States;
using Dargon.Courier.StateReplicationTier.Vox;

namespace Dargon.Courier.StateReplicationTier.Replicas;

public class StateUpdateProcessor<TState, TSnapshot, TDelta> where TState : class, IState where TSnapshot : IStateSnapshot where TDelta : class, IStateDelta {
   private readonly IStateView<TState, TSnapshot, TDelta> stateView;

   private readonly ConcurrentQueue<StateUpdateDto> queuedSnapshotUpdates = new();
   private readonly ConcurrentQueue<StateUpdateDto> queuedDeltaUpdates = new();
   private readonly SortedList<int, SortedList<int, StateUpdateDto>> unprocessedUpdatesPerEpoch = new();
   private readonly AsyncLatch initialStateSnapshotQueuedLatch = new AsyncLatch();
   private readonly AsyncLatch initialStateSnapshotLoadedLatch = new AsyncLatch();
   private int currentEpoch = -1;
   private int lastAppliedSeqInEpoch = -1;
   private int version = -1;

   public StateUpdateProcessor(IStateView<TState, TSnapshot, TDelta> stateView) {
      this.stateView = stateView;
   }

   public int Version => version;
   public bool HasLoadedInitialState => initialStateSnapshotLoadedLatch.IsSignalled;
   public bool HasInboundUpdates => queuedSnapshotUpdates.Count > 0 || queuedDeltaUpdates.Count > 0;

   public async Task WaitForAndProcessInitialStateUpdateAsync() {
      await initialStateSnapshotQueuedLatch.WaitAsync();
      ProcessQueuedUpdates();
      initialStateSnapshotLoadedLatch.IsSignalled.AssertIsTrue();
   }

   public void Enqueue(StateUpdateDto stateUpdate) {
      if (stateUpdate.IsSnapshot) {
         queuedSnapshotUpdates.Enqueue(stateUpdate);
         initialStateSnapshotQueuedLatch.TrySet();
      } else {
         queuedDeltaUpdates.Enqueue(stateUpdate);
      }
   }

   public void ProcessQueuedUpdates() {
      while (queuedSnapshotUpdates.Count > 0) {
         var update = queuedSnapshotUpdates.DequeueOrThrow();
         update.IsSnapshot.AssertIsTrue();

         if (update.Version.Epoch > currentEpoch || (update.Version.Epoch == currentEpoch && update.Version.Seq > lastAppliedSeqInEpoch)) {
            stateView.LoadSnapshot((TSnapshot)update.Payload, update.Version);
            currentEpoch = update.Version.Epoch;
            lastAppliedSeqInEpoch = update.Version.Seq;
            version++;
            unprocessedUpdatesPerEpoch.TryAdd(currentEpoch, new());
         }
      }

      while (queuedDeltaUpdates.Count > 0) {
         var update = queuedDeltaUpdates.DequeueOrThrow();
         update.IsSnapshot.AssertIsFalse();
         update.IsOutOfBand.AssertIsFalse();

         if (update.Version.Epoch < currentEpoch || (update.Version.Epoch == currentEpoch && update.Version.Seq <= lastAppliedSeqInEpoch)) continue; // throw away
         if (!unprocessedUpdatesPerEpoch.TryGetValue(update.Version.Epoch, out var seqToUpdate)) {
            seqToUpdate = unprocessedUpdatesPerEpoch[update.Version.Epoch] = new();
         }
         seqToUpdate.Add(update.Version.Seq, update);
      }

      if (currentEpoch == -1) {
         // no epoch started yet
         return;
      }

      var unprocessedUpdatesOfCurrentEpoch = unprocessedUpdatesPerEpoch[currentEpoch];
      while (unprocessedUpdatesOfCurrentEpoch.Remove(lastAppliedSeqInEpoch + 1, out var updateToApply)) {
         if (updateToApply.IsSnapshot) {
            stateView.LoadSnapshot((TSnapshot)updateToApply.Payload, updateToApply.Version);
         } else {
            stateView.TryApplyDelta((TDelta)updateToApply.Payload, updateToApply.Version).AssertIsTrue();
            lastAppliedSeqInEpoch = updateToApply.Version.Seq;
         }
         version++;
      }

      CullStaleUnprocessedUpdates();

      if (currentEpoch >= 0 && !initialStateSnapshotLoadedLatch.IsSignalled) {
         initialStateSnapshotLoadedLatch.SetOrThrow();
      }
   }

   private void CullStaleUnprocessedUpdates() {
      var unprocessedUpdatesPerEpochKeys = unprocessedUpdatesPerEpoch.Keys.ToArray();

      foreach (var epochId in unprocessedUpdatesPerEpochKeys) {
         if (epochId < currentEpoch) {
            unprocessedUpdatesPerEpoch.Remove(epochId);
            continue;
         }

         if (epochId == currentEpoch) {
            var unprocessedUpdatesOfEpoch = unprocessedUpdatesPerEpoch[epochId];
            while (unprocessedUpdatesOfEpoch.Count > 0) {
               var lowestDeltaSeqId = unprocessedUpdatesOfEpoch.Keys[0];
               if (lowestDeltaSeqId <= lastAppliedSeqInEpoch) {
                  unprocessedUpdatesOfEpoch.Remove(lowestDeltaSeqId);
               } else {
                  break;
               }
            }
         }

         return;
      }
   }
}