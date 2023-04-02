using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Commons.AsyncPrimitives;
using Dargon.Commons.Channels;
using Dargon.Courier.PeeringTier;
using Dargon.Courier.PubSubTier.Publishers;
using Dargon.Courier.PubSubTier.Subscribers;
using Dargon.Courier.PubSubTier.Vox;
using Dargon.Courier.ServiceTier.Client;
using Dargon.Courier.StateReplicationTier.Primaries;
using Dargon.Courier.StateReplicationTier.States;
using Dargon.Courier.StateReplicationTier.Vox;

namespace Dargon.Courier.StateReplicationTier.Replicas {
   public class RemoteStateSubscriber<TState, TSnapshot, TDelta> where TState : class, IState where TSnapshot : IStateSnapshot where TDelta : class, IStateDelta {
      private readonly RemoteServiceProxyContainer remoteServiceProxyContainer;
      private readonly Subscriber subscriber;
      private readonly PeerContext remote;
      private readonly Guid topicId;
      private readonly StateUpdateProcessor<TState, TSnapshot, TDelta> updateProcessor;

      public RemoteStateSubscriber(RemoteServiceProxyContainer remoteServiceProxyContainer, Subscriber subscriber, PeerContext remote, Guid topicId, StateUpdateProcessor<TState, TSnapshot, TDelta> updateProcessor) {
         this.remoteServiceProxyContainer = remoteServiceProxyContainer;
         this.subscriber = subscriber;
         this.remote = remote;
         this.topicId = topicId;
         this.updateProcessor = updateProcessor;
      }

      public async Task InitializeAsync() {
         await subscriber.SubscribeToRemoteTopicAsync(topicId, remote, HandleRemotePublishAsync);
         var primaryStateService = remoteServiceProxyContainer.Get<IPrimaryStateService<TState, TSnapshot, TDelta>>(topicId, remote);
         var update = await primaryStateService.GetOutOfBandSnapshotUpdateOfLatestStateAsync();
         updateProcessor.Enqueue(update);
      }

      private Task HandleRemotePublishAsync(PubSubNotification notification) {
         var stateUpdate = (StateUpdateDto)notification.Payload;
         updateProcessor.Enqueue(stateUpdate);
         return Task.CompletedTask;
      }
   }

   public class StateUpdateProcessor<TState, TSnapshot, TDelta> where TState : IState where TSnapshot : IStateSnapshot where TDelta : class, IStateDelta {
      private readonly TState state;
      private readonly IStateDeltaOperations<TState, TSnapshot, TDelta> ops;

      private readonly ConcurrentQueue<StateUpdateDto> queuedSnapshotUpdates = new();
      private readonly ConcurrentQueue<StateUpdateDto> queuedDeltaUpdates = new();
      private readonly SortedList<int, SortedList<int, StateUpdateDto>> unprocessedUpdatesPerEpoch = new();
      private readonly AsyncLatch initialStateSnapshotQueuedLatch = new AsyncLatch();
      private readonly AsyncLatch initialStateSnapshotLoadedLatch = new AsyncLatch();
      private int currentEpoch = -1;
      private int lastAppliedSeqInEpoch = -1;
      private int version = -1;

      public StateUpdateProcessor(TState state, IStateDeltaOperations<TState, TSnapshot, TDelta> ops) {
         this.state = state;
         this.ops = ops;
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

            if (update.SnapshotEpoch > currentEpoch || (update.SnapshotEpoch == currentEpoch && update.DeltaSeq > lastAppliedSeqInEpoch)) {
               ops.LoadSnapshot(state, (TSnapshot)update.Payload);
               currentEpoch = update.SnapshotEpoch;
               lastAppliedSeqInEpoch = update.DeltaSeq;
               version++;
               unprocessedUpdatesPerEpoch.TryAdd(currentEpoch, new());
            }
         }

         while (queuedDeltaUpdates.Count > 0) {
            var update = queuedDeltaUpdates.DequeueOrThrow();
            update.IsSnapshot.AssertIsFalse();
            update.IsOutOfBand.AssertIsFalse();

            if (update.SnapshotEpoch < currentEpoch || (update.SnapshotEpoch == currentEpoch && update.DeltaSeq <= lastAppliedSeqInEpoch)) continue; // throw away
            if (!unprocessedUpdatesPerEpoch.TryGetValue(update.SnapshotEpoch, out var seqToUpdate)) {
               seqToUpdate = unprocessedUpdatesPerEpoch[update.SnapshotEpoch] = new();
            }
            seqToUpdate.Add(update.DeltaSeq, update);
         }

         if (currentEpoch == -1) {
            // no epoch started yet
            return;
         }

         var unprocessedUpdatesOfCurrentEpoch = unprocessedUpdatesPerEpoch[currentEpoch];
         while (unprocessedUpdatesOfCurrentEpoch.Remove(lastAppliedSeqInEpoch + 1, out var updateToApply)) {
            if (updateToApply.IsSnapshot) {
               ops.LoadSnapshot(state, (TSnapshot)updateToApply.Payload);
            } else {
               ops.TryApplyDelta(state, (TDelta)updateToApply.Payload).AssertIsTrue();
               lastAppliedSeqInEpoch = updateToApply.DeltaSeq;
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
}